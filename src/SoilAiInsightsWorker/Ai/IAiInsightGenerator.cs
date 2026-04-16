using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Ai;

public interface IAiInsightGenerator
{
    Task<RecommendationAiPayload> GenerateRecommendationAsync(
        string systemPrompt,
        string userJsonPayload,
        CancellationToken cancellationToken);

    Task<ForecastAiPayload> GenerateForecastAsync(
        string systemPrompt,
        string userJsonPayload,
        int horizonHours,
        CancellationToken cancellationToken);
}
