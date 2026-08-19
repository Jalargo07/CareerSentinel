namespace CareerSentinel.Models;

/// <summary>
/// Resultado de un ciclo de búsqueda de empleos.
/// </summary>
public record SearchResult
{
    /// <summary>Total de ofertas procesadas (con descripción válida).</summary>
    public int TotalProcessed { get; init; }

    /// <summary>Ofertas que superaron el umbral de score.</summary>
    public int Matched { get; init; }

    /// <summary>Ofertas guardadas en Notion exitosamente.</summary>
    public int Saved { get; init; }
}
