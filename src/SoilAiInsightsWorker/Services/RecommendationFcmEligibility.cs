using System.Globalization;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Services;

/// <summary>
/// Aligns with <see cref="Ai.PromptBuilders.RecommendationPromptBuilder"/> allowed values:
/// priority high|critical and urgency today|within_hours|immediate → push to the device owner.
/// </summary>
public static class RecommendationFcmEligibility
{
    public static bool ShouldSendPush(RecommendationAiPayload p)
    {
        var priority = p.Priority.Trim().ToLowerInvariant();
        if (priority is not ("high" or "critical"))
            return false;

        var urgency = p.Urgency.Trim().ToLowerInvariant();
        return urgency is "today" or "within_hours" or "immediate";
    }
}
