using CareerSentinel.Configuration;
using CareerSentinel.Models;

namespace CareerSentinel.Services;

public interface ILlmService
{
    Task<JobAnalysis?> AnalyzeJobAsync(string jobTitle, string jobDescription, CancellationToken ct = default);
    Task<EvaluationResult?> EvaluateJobAsync(string jobTitle, JobAnalysis analysis, CandidateProfile candidate, CancellationToken ct = default);
    Task<List<EvaluationResult>> EvaluateBatchAsync(BatchEvaluationRequest request, CancellationToken ct = default);
    Task<List<JobAnalysis?>> AnalyzeBatchAsync(
        List<(string Title, string Description)> offers,
        CancellationToken ct = default);
}
