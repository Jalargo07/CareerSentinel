using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CareerSentinel.Services;

public class OpenCodeGoService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenCodeGoService> _logger;
    private readonly OpenCodeGoSettings _settings;
    private readonly ScoringSettings _scoringSettings;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly AsyncFileLogger _evaluationLogger;

    private static readonly Random _jitterRandom = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public OpenCodeGoService(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        ILogger<OpenCodeGoService> logger,
        AsyncFileLogger evaluationLogger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenCodeGo");
        _logger = logger;
        _settings = settings.Value.OpenCodeGo;
        _scoringSettings = settings.Value.Scoring;
        _evaluationLogger = evaluationLogger;

        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                _scoringSettings.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * (1 + _jitterRandom.NextDouble() * 0.2)),
                onRetry: (outcome, delay, retryCount, _) =>
                {
                    _logger.LogWarning(
                        "Reintento API {RetryCount}/{MaxRetries} tras {Delay}s (StatusCode: {StatusCode})",
                        retryCount, _scoringSettings.MaxRetries, delay.TotalSeconds, outcome.Result?.StatusCode);
                });
    }

    public async Task<JobAnalysis?> AnalyzeJobAsync(string jobTitle, string jobDescription, CancellationToken ct = default)
    {
        var prompt = BuildAnalysisPrompt(jobDescription);

        _logger.LogInformation("[API Paso1] Analizando: {Title} ({Length} chars)", jobTitle, jobDescription.Length);

        try
        {
            var response = await CallOpenAiApiAsync(prompt, ct);
            if (response is null) return null;

            var result = ParseJobAnalysis(response);
            if (result is not null)
            {
                result = result with { DescripcionOriginal = jobDescription };
                _logger.LogInformation(
                    "[API Paso1] Completado: {Title} | Válido: {Valid} | Techs: {TechCount}",
                    jobTitle, result.EsTextoValido, result.TecnologiasClave.Count);

                // Escribir log de evaluaciones (Paso 1)
                try
                {
                    var entry = $"\n=== API Paso1: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n";
                    entry += $"Título: {jobTitle}\n";
                    entry += $"Descripción ({jobDescription.Length} chars): {(jobDescription.Length > 300 ? jobDescription[..300] + "..." : jobDescription)}\n";
                    entry += $"Análisis: Válido={result.EsTextoValido}, Techs=[{string.Join(", ", result.TecnologiasClave)}]\n\n";
                    await _evaluationLogger.AppendAsync(entry).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "No se pudo escribir en log de evaluaciones (Paso1)");
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API Paso1] Error analizando: {Title}", jobTitle);
            return null;
        }
    }

    public async Task<EvaluationResult?> EvaluateJobAsync(string jobTitle, JobAnalysis analysis, CandidateProfile candidate, CancellationToken ct = default)
    {
        var results = await EvaluateBatchAsync(new BatchEvaluationRequest
        {
            Offers = new List<OfferToEvaluate>
            {
                new()
                {
                    Title = analysis.Titulo,
                    Company = analysis.Empresa,
                    Modality = analysis.Modalidad,
                    Location = analysis.Ubicacion,
                    Seniority = analysis.SeniorityRequerido,
                    ExperienceYears = analysis.AnosExperiencia,
                    Technologies = analysis.TecnologiasClave,
                    Description = analysis.DescripcionOriginal
                }
            },
            Candidate = new CandidateInfo
            {
                Level = candidate.Level,
                YearsExperience = candidate.YearsExperience.ToString(),
                PreferredModality = candidate.PreferredModality,
                PreferredRegions = candidate.PreferredRegions.ToList(),
                Skills = candidate.CoreSkills.ToList(),
                CvDescription = candidate.CvDescription
            }
        }, ct);

        return results.FirstOrDefault();
    }

    public async Task<List<EvaluationResult>> EvaluateBatchAsync(BatchEvaluationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("[API Paso2] Evaluando batch de {Count} ofertas", request.Offers.Count);

        var prompt = BuildBatchEvaluationPrompt(request);
        var results = new List<EvaluationResult>();

        try
        {
            var response = await CallOpenAiApiAsync(prompt, ct, isBatch: true);
            if (response is null)
            {
                _logger.LogWarning("[API Paso2] Respuesta nula, generando scores 0");
                return request.Offers.Select(_ => new EvaluationResult
                {
                    Score = 0,
                    Match = false,
                    Razon = "Error en la API"
                }).ToList();
            }

            results = ParseBatchResponse(response, request.Offers.Count);

            // Escribir log de evaluaciones (batch)
            try
            {
                var entry = $"\n=== API Batch evaluado: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n";
                foreach (var offer in request.Offers)
                {
                    entry += $"Oferta: {offer.Title}\n";
                    entry += $"Empresa: {offer.Company}\n";
                    entry += $"Descripción ({offer.Description?.Length ?? 0} chars): {(offer.Description?.Length > 200 ? offer.Description[..200] + "..." : offer.Description)}\n";
                }
                entry += $"Respuesta API: {response}\n\n";
                await _evaluationLogger.AppendAsync(entry).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo escribir en log de evaluaciones");
            }

            _logger.LogInformation("[API Paso2] Batch completado: {Count} evaluaciones", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API Paso2] Error en batch");
            results = request.Offers.Select(_ => new EvaluationResult
            {
                Score = 0,
                Match = false,
                Razon = "Error en la API"
            }).ToList();
        }

        return results;
    }

    private async Task<string?> CallOpenAiApiAsync(string prompt, CancellationToken ct, bool isBatch = false)
    {
        var requestBody = new
        {
            model = _settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = "Eres un evaluador técnico de empleo. Responde SOLO con JSON válido, sin markdown." },
                new { role = "user", content = prompt }
            },
            temperature = 0.0,
            max_tokens = isBatch ? _settings.MaxTokensBatch : 2000,
            response_format = new { type = "json_object" }
        };

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions";
        var response = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsJsonAsync(url, requestBody, ct));

        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<OpenAiResponse>(JsonOptions, ct);
        return apiResponse?.Choices?.FirstOrDefault()?.Message?.Content;
    }

    private static string BuildAnalysisPrompt(string jobDescription)
    {
        return $@"Extrae datos de esta oferta de trabajo. Responde SOLO con JSON.

TEXTO:
""""""{jobDescription}""""""

REGLAS:
- Si no es oferta real (login, menú, <80 palabras): ""es_texto_valido"": false
- NO inventes empresas, ubicaciones ni tecnologías

JSON:
{{
  ""es_texto_valido"": true,
  ""titulo"": ""Nombre del puesto"",
  ""empresa"": ""Empresa o Unspecified"",
  ""modalidad"": ""Remoto|Híbrido|Presencial|No especifica"",
  ""ubicacion"": ""Ciudad, País o Unspecified"",
  ""seniority_requerido"": ""Junior|Mid|Senior|Lead|No especifica"",
  ""anos_experiencia"": ""número o No especifica"",
  ""tecnologias_clave"": [""tech1"", ""tech2""],
  ""resumen"": ""Resumen en 2 oraciones""
}}";
    }

    private static string BuildBatchEvaluationPrompt(BatchEvaluationRequest request)
    {
        var offersText = string.Join("\n\n", request.Offers.Select((o, i) =>
            $"OFERTA {i + 1}:\n" +
            $"Título: {o.Title}\n" +
            $"Empresa: {o.Company}\n" +
            $"Modalidad: {o.Modality}\n" +
            $"Ubicación: {o.Location}\n" +
            $"Seniority: {o.Seniority}\n" +
            $"Años: {o.ExperienceYears}\n" +
            $"Techs: {string.Join(", ", o.Technologies)}\n" +
            $"Descripción: {o.Description}"));

        var cvLine = string.IsNullOrWhiteSpace(request.Candidate.CvDescription)
            ? string.Empty
            : $"\n- CV Descripción: {request.Candidate.CvDescription}";

        return $@"Evalúa estas {request.Offers.Count} ofertas contra el perfil del candidato.

PERFIL:
- Nivel: {request.Candidate.Level} | Años: {request.Candidate.YearsExperience}
- Modalidad: {request.Candidate.PreferredModality}
- Regiones: {string.Join(", ", request.Candidate.PreferredRegions)}
- Skills: {string.Join(", ", request.Candidate.Skills)}{cvLine}

OFERTAS:
{offersText}

Para cada oferta aplica la PRIMERA regla que coincida (R1-R5) y responde con score y match:
- R1: Texto invalido o 0 tecnologias => score 0, match false
- R2: (Presencial o Híbrido) y ubicación fuera de regiones preferidas => score 10, match false
- R3: Oferta Senior y candidato Junior => score 25, match false
- R4: Candidato tiene MENOS de 2 tecnologías de la oferta => score 30, match false
- R5: Cumple base (techs>=2, seniority compatible, región OK o remoto) => score 85, match true

REGLA CRÍTICA: match es true SOLO si score == 85. Usar SOLO estos scores: 0, 10, 25, 30, 85.

JSON con wrapper:
{{
  ""evaluations"": [
    {{
      ""indice"": 1,
      ""score"": 85,
      ""match"": true,
      ""cumple"": [""cosas buenas""],
      ""nocumple"": [""cosas que faltan""],
      ""razon"": ""Razón breve""
    }}
  ]
}}

REGLAS:
- ""cumple"": lista de cosas buenas de la oferta (salario, benefits, modalidad, techs, etc.)
- ""nocumple"": lista de cosas que no cumple o faltan (sin salario, presencial, senior, etc.)
- Si algo no aplica, usar lista vacía []";
    }

    private List<EvaluationResult> ParseBatchResponse(string response, int expectedCount)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("evaluations", out var evalArray))
            {
                var results = new List<EvaluationResult>();
                foreach (var item in evalArray.EnumerateArray())
                {
                    var eval = JsonSerializer.Deserialize<EvaluationResult>(item.GetRawText(), JsonOptions);
                    if (eval is not null)
                    {
                        // Respetar la decisión del LLM: match=true SOLO si score==85
                        results.Add(eval with
                        {
                            Match = eval.Score == 85,
                            Cumple = eval.Cumple ?? [],
                            NoCumple = eval.NoCumple ?? [],
                            Razon = eval.Razon ?? string.Empty
                        });
                    }
                }

                while (results.Count < expectedCount)
                    results.Add(new EvaluationResult { Score = 0, Match = false, Razon = "No evaluado" });

                return results.Take(expectedCount).ToList();
            }
        }
        catch (JsonException) { }

        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(response, @"""evaluations""\s*:\s*\[[\s\S]*?\]|\[[\s\S]*?\]");
            if (match.Success)
            {
                var results = JsonSerializer.Deserialize<List<EvaluationResult>>(match.Value, JsonOptions);
                if (results is not null)
                {
                    while (results.Count < expectedCount)
                        results.Add(new EvaluationResult { Score = 0, Match = false, Razon = "No evaluado" });

                    return results.Take(expectedCount).Select(r => r with
                    {
                        // Respetar la decisión del LLM: match=true SOLO si score==85
                        Match = r.Score == 85,
                        Cumple = r.Cumple ?? [],
                        NoCumple = r.NoCumple ?? [],
                        Razon = r.Razon ?? string.Empty
                    }).ToList();
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Error parseando fallback array de evaluaciones");
        }

        return Enumerable.Repeat(new EvaluationResult { Score = 0, Match = false, Razon = "Error parsing" }, expectedCount).ToList();
    }

    private JobAnalysis? ParseJobAnalysis(string rawResponse)
    {
        try
        {
            var result = JsonJobParser.ParseJobAnalysis(rawResponse);
            if (result is not null)
                return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parseando análisis de API vía JsonJobParser");
        }

        _logger.LogError("No se pudo parsear la respuesta de API para análisis: {Response}",
            rawResponse[..Math.Min(200, rawResponse.Length)]);
        return null;
    }

    private static string BuildAnalysisBatchPrompt(List<(string Title, string Description)> offers)
    {
        var offersText = string.Join("\n\n", offers.Select((o, i) =>
            $"OFERTA {i + 1} <<<>>>\n" +
            $"Título: {o.Title}\n" +
            $"Descripción:\n{o.Description}\n" +
            $"<<<FIN OFERTA {i + 1}>>>"));

        return $@"Extrae datos de estas {offers.Count} ofertas de trabajo. Responde SOLO con JSON.

REGLAS CRÍTICAS:
- Cada oferta es INDEPENDIENTE. NO mezcles datos entre ofertas.
- NO inventes empresas, ubicaciones ni tecnologías. Solo lo que aparezca en el texto.
- Si una oferta no es real (login, menú, <80 palabras): ""es_texto_valido"": false
- Devuelve EXACTAMENTE {offers.Count} objetos en el array, uno por oferta.
- Cada objeto DEBE tener un campo ""indice"" que coincida con el número de oferta (1, 2, 3...).

OFERTAS:
{offersText}

JSON con wrapper:
{{
  ""analyses"": [
    {{
      ""indice"": 1,
      ""es_texto_valido"": true,
      ""titulo"": ""Nombre del puesto"",
      ""empresa"": ""Empresa o Unspecified"",
      ""modalidad"": ""Remoto|Híbrido|Presencial|No especifica"",
      ""ubicacion"": ""Ciudad, País o Unspecified"",
      ""seniority_requerido"": ""Junior|Mid|Senior|Lead|No especifica"",
      ""anos_experiencia"": ""número o No especifica"",
      ""tecnologias_clave"": [""tech1"", ""tech2""],
      ""resumen"": ""Resumen en 2 oraciones""
    }}
  ]
}}";
    }

    private List<JobAnalysis?> ParseAnalysisBatchResponse(string response, int expectedCount)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("analyses", out var analysesArray))
            {
                var results = new List<JobAnalysis?>();
                foreach (var item in analysesArray.EnumerateArray())
                {
                    var analysis = JsonSerializer.Deserialize<JobAnalysis>(item.GetRawText(), JsonOptions);
                    results.Add(analysis);
                }

                while (results.Count < expectedCount)
                    results.Add(null);

                return results.Take(expectedCount).ToList();
            }
        }
        catch (JsonException) { }

        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(response, @"""indices""\s*:\s*\[[\s\S]*?\]|\[[\s\S]*?\]");
            if (match.Success)
            {
                var arr = JsonSerializer.Deserialize<List<JobAnalysis>>(match.Value, JsonOptions);
                if (arr is not null)
                {
                    var resultArr = arr.Cast<JobAnalysis?>().ToList();
                    while (resultArr.Count < expectedCount)
                        resultArr.Add(null);
                    return resultArr.Take(expectedCount).ToList();
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Error parseando fallback array de análisis");
        }

        return Enumerable.Repeat<JobAnalysis?>(null, expectedCount).ToList();
    }

    public async Task<List<JobAnalysis?>> AnalyzeBatchAsync(
        List<(string Title, string Description)> offers,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[API Paso1 batch] Analizando {Count} ofertas", offers.Count);

        var prompt = BuildAnalysisBatchPrompt(offers);

        try
        {
            var response = await CallOpenAiApiAsync(prompt, ct, isBatch: true);
            if (response is null)
            {
                _logger.LogWarning("[API Paso1 batch] Respuesta nula");
                return offers.Select(_ => (JobAnalysis?)null).ToList();
            }

            var results = ParseAnalysisBatchResponse(response, offers.Count);

            _logger.LogInformation("[API Paso1 batch] Completado: {Count} análisis", results.Count(r => r is not null));
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API Paso1 batch] Error");
            return offers.Select(_ => (JobAnalysis?)null).ToList();
        }
    }

    private sealed class OpenAiResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
