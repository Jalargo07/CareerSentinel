using System.Text.Json.Serialization;

namespace CareerSentinel.Models;

/// <summary>
/// Resultado del Paso 1: Extracción de datos clave de la oferta laboral.
/// </summary>
public record JobAnalysis
{
    [JsonPropertyName("es_texto_valido")]
    public bool EsTextoValido { get; init; }

    [JsonPropertyName("titulo")]
    public string Titulo { get; init; } = string.Empty;

    [JsonPropertyName("empresa")]
    public string Empresa { get; init; } = string.Empty;

    [JsonPropertyName("modalidad")]
    public string Modalidad { get; init; } = string.Empty; // "Remoto", "Híbrido", "Presencial", "No especifica"

    [JsonPropertyName("ubicacion")]
    public string Ubicacion { get; init; } = string.Empty; // "Medellín, Colombia", "Mendoza, Argentina", etc.

    [JsonPropertyName("seniority_requerido")]
    public string SeniorityRequerido { get; init; } = string.Empty; // "Junior", "Mid", "Senior", "Lead", "No especifica"

    [JsonPropertyName("anos_experiencia")]
    public string AnosExperiencia { get; init; } = string.Empty; // "3", "5+", "No especifica"

    [JsonPropertyName("tecnologias_clave")]
    public List<string> TecnologiasClave { get; init; } = new();

    [JsonPropertyName("resumen")]
    public string Resumen { get; init; } = string.Empty; // Resumen de 1 línea

    [JsonPropertyName("responsabilidades")]
    public List<string> Responsabilidades { get; init; } = new();

    [JsonPropertyName("requisitos_deseados")]
    public List<string> RequisitosDeseados { get; init; } = new();

    [JsonPropertyName("descripcion_original")]
    public string DescripcionOriginal { get; init; } = string.Empty;

    [JsonPropertyName("indice")]
    public int Indice { get; init; }
}
