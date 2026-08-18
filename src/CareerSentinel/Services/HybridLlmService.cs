using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;

namespace CareerSentinel.Services;

public class HybridLlmService : ILlmService
{
    private readonly LocalLlmService _localService;
    private readonly OpenCodeGoService _apiService;
    private readonly ILogger<HybridLlmService> _logger;

    public HybridLlmService(
        LocalLlmService localService,
        OpenCodeGoService apiService,
        ILogger<HybridLlmService> logger)
    {
        _localService = localService;
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<JobAnalysis?> AnalyzeJobAsync(string jobTitle, string jobDescription, CancellationToken ct = default)
    {
        // Paso 1: Usa Ollama local para extracción
        _logger.LogInformation("[Hybrid] Paso1 usando Ollama local para: {Title}", jobTitle);
        return await _localService.AnalyzeJobAsync(jobTitle, jobDescription, ct);
    }

    public async Task<EvaluationResult?> EvaluateJobAsync(string jobTitle, JobAnalysis analysis, CandidateProfile candidate, CancellationToken ct = default)
    {
        // Paso 2: Usa API para evaluación
        _logger.LogInformation("[Hybrid] Paso2 usando API para: {Title}", jobTitle);
        return await _apiService.EvaluateJobAsync(jobTitle, analysis, candidate, ct);
    }

    public async Task<List<JobAnalysis?>> AnalyzeBatchAsync(
        List<(string Title, string Description)> offers,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[Hybrid] Paso1 batch delegando a local para {Count} ofertas", offers.Count);
        return await _localService.AnalyzeBatchAsync(offers, ct);
    }

    public async Task<List<EvaluationResult>> EvaluateBatchAsync(BatchEvaluationRequest request, CancellationToken ct = default)
    {
        // Batch: Usa API
        _logger.LogInformation("[Hybrid] Batch usando API para {Count} ofertas", request.Offers.Count);
        return await _apiService.EvaluateBatchAsync(request, ct);
    }
}
