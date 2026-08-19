using System.Text.Json;
using CareerSentinel.Models;

namespace CareerSentinel.Services;

/// <summary>
/// Helper estático compartido para parsear respuestas JSON de LLMs en modelos del dominio.
/// Centraliza la lógica de extracción de campos con fallback entre snake_case y camelCase.
/// </summary>
public static class JsonJobParser
{
    /// <summary>
    /// Extrae un valor booleano de un JsonElement, intentando múltiples nombres de propiedad
    /// en orden. Soporta true/false, "true"/"yes"/"1", y 0/1 numéricos.
    /// </summary>
    /// <param name="element">Elemento raíz o contenedor.</param>
    /// <param name="propertyNames">Nombres de propiedad a intentar en orden (ej: "es_texto_valido", "valid").</param>
    /// <returns>El valor booleano, o false si ninguno se encontró.</returns>
    public static bool GetBoolProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!element.TryGetProperty(name, out var prop)) continue;

            return prop.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => prop.GetString()?.ToLowerInvariant() is "true" or "yes" or "1",
                JsonValueKind.Number => prop.GetInt32() == 1,
                _ => false
            };
        }

        return false;
    }

    /// <summary>
    /// Extrae un valor string de un JsonElement, intentando múltiples nombres de propiedad
    /// en orden. Retorna string.Empty si ninguno se encontró.
    /// </summary>
    /// <param name="element">Elemento raíz o contenedor.</param>
    /// <param name="propertyNames">Nombres de propiedad a intentar en orden (ej: "titulo", "title").</param>
    /// <returns>El valor string, o string.Empty si ninguno se encontró.</returns>
    public static string GetStringProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Extrae un array de strings de un JsonElement, intentando múltiples nombres de propiedad
    /// en orden. Filtra valores vacíos o nulos.
    /// </summary>
    /// <param name="element">Elemento raíz o contenedor.</param>
    /// <param name="propertyNames">Nombres de propiedad a intentar en orden (ej: "tecnologias_clave", "techs").</param>
    /// <returns>Lista de strings no vacíos, o lista vacía si ninguno se encontró.</returns>
    public static List<string> GetStringArrayProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!element.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array)
                continue;

            var result = new List<string>();
            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } value)
                    result.Add(value);
            }

            if (result.Count > 0)
                return result;
        }

        return new List<string>();
    }

    /// <summary>
    /// Parsea un objeto JSON en un <see cref="JobAnalysis"/>, extrayendo cada campo
    /// con fallback entre nombres en snake_case y camelCase.
    /// Este método NO lanza excepciones; retorna null si el parsing falla completamente.
    /// </summary>
    /// <param name="root">Elemento raíz del JsonDocument.</param>
    /// <returns>Un JobAnalysis parseado, o null si falla.</returns>
    public static JobAnalysis ParseJobAnalysis(JsonElement root)
    {
        var isValid = GetBoolProperty(root, "es_texto_valido", "valid");
        var titulo = GetStringProperty(root, "titulo", "title");
        var empresa = GetStringProperty(root, "empresa", "company");
        var modalidad = GetStringProperty(root, "modalidad", "modality");
        var ubicacion = GetStringProperty(root, "ubicacion", "location");
        var seniority = GetStringProperty(root, "seniority_requerido", "seniority");
        var anos = GetStringProperty(root, "anos_experiencia", "experience_years");
        var resumen = GetStringProperty(root, "resumen", "summary");
        var tecnologias = GetStringArrayProperty(root, "tecnologias_clave", "techs");

        return new JobAnalysis
        {
            EsTextoValido = isValid,
            Titulo = titulo,
            Empresa = empresa,
            Modalidad = modalidad,
            Ubicacion = ubicacion,
            SeniorityRequerido = seniority,
            AnosExperiencia = anos,
            TecnologiasClave = tecnologias,
            Resumen = resumen,
            Responsabilidades = new List<string>(),
            RequisitosDeseados = new List<string>()
        };
    }

    /// <summary>
    /// Parsea una cadena JSON cruda en un <see cref="JobAnalysis"/>.
    /// Primero intenta parseo directo; si falla, extrae el primer objeto JSON
    /// mediante regex como fallback.
    /// </summary>
    /// <param name="rawResponse">Respuesta cruda del LLM.</param>
    /// <returns>JobAnalysis parseado, o null si no se pudo parsear.</returns>
    public static JobAnalysis? ParseJobAnalysis(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return null;

        // Intento 1: Parseo JSON directo
        try
        {
            using var doc = JsonDocument.Parse(rawResponse);
            return ParseJobAnalysis(doc.RootElement);
        }
        catch (JsonException)
        {
            // Continúa al fallback
        }

        // Intento 2: Extraer primer objeto JSON vía regex
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                rawResponse,
                @"\{[^{}]*""es_texto_valido""[^{}]*\}",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (match.Success)
            {
                using var doc = JsonDocument.Parse(match.Value);
                return ParseJobAnalysis(doc.RootElement);
            }
        }
        catch (JsonException)
        {
            // Falló el fallback
        }

        return null;
    }
}
