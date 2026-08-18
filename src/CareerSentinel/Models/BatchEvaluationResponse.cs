namespace CareerSentinel.Models;

public record BatchEvaluationResponse
{
    public List<EvaluationResult> Evaluations { get; init; } = new();
}
