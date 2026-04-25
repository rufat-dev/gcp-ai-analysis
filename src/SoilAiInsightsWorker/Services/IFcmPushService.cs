using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Services;

public interface IFcmPushService
{
    /// <summary>
    /// Sends an urgent-recommendation notification when <see cref="RecommendationFcmEligibility"/> passes.
    /// No-ops when disabled, misconfigured, or token missing.
    /// </summary>
    Task TrySendUrgentRecommendationAsync(
        string? userId,
        string recommendationId,
        string deviceId,
        RecommendationAiPayload payload,
        CancellationToken cancellationToken);
}
