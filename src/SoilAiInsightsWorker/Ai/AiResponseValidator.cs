using System.Text.Json;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Ai;

public static class AiResponseValidator
{
    private static readonly HashSet<string> Priorities = new(StringComparer.OrdinalIgnoreCase)
        { "low", "medium", "high", "critical" };

    private static readonly HashSet<string> Urgencies = new(StringComparer.OrdinalIgnoreCase)
        { "monitor", "today", "within_hours", "immediate" };

    private static readonly HashSet<string> Scopes = new(StringComparer.OrdinalIgnoreCase)
        { "current_state", "anomaly_and_range", "trend_and_state" };

    private static readonly HashSet<string> Bands = new(StringComparer.OrdinalIgnoreCase)
        { "low", "normal", "high", "optimal" };

    private static readonly HashSet<string> TriState = new(StringComparer.OrdinalIgnoreCase)
        { "low", "medium", "high" };

    public static RecommendationAiPayload SanitizeRecommendation(RecommendationAiPayload p)
    {
        if (!Priorities.Contains(p.Priority))
            p.Priority = "low";
        if (!Urgencies.Contains(p.Urgency))
            p.Urgency = "monitor";
        if (!Scopes.Contains(p.ReasoningScope))
            p.ReasoningScope = "current_state";
        p.Confidence = Clamp01(p.Confidence);
        p.Title = Truncate(p.Title, 500);
        p.Summary = Truncate(p.Summary, 4000);
        p.Recommendation = Truncate(p.Recommendation, 8000);
        p.ProbableCause = Truncate(p.ProbableCause, 4000);
        p.SupportingFacts = p.SupportingFacts.Select(s => Truncate(s, 500)).Where(s => !string.IsNullOrWhiteSpace(s)).Take(32).ToList();
        return p;
    }

    public static ForecastAiPayload SanitizeForecast(ForecastAiPayload p)
    {
        p.PredictedTemperatureBand = NormalizeBand(p.PredictedTemperatureBand);
        p.PredictedMoistureBand = NormalizeBand(p.PredictedMoistureBand);
        p.PredictedConductivityBand = NormalizeBand(p.PredictedConductivityBand);
        p.PredictedPhBand = NormalizeBand(p.PredictedPhBand);
        if (!TriState.Contains(p.AnomalyRisk))
            p.AnomalyRisk = "low";
        if (!TriState.Contains(p.StressRisk))
            p.StressRisk = "low";
        p.Confidence = Clamp01(p.Confidence);
        p.PredictedRiskScore = Math.Clamp(p.PredictedRiskScore, 0, 100);
        p.Title = Truncate(p.Title, 500);
        p.ForecastSummary = Truncate(p.ForecastSummary, 4000);
        p.RecommendedPreventiveAction = Truncate(p.RecommendedPreventiveAction, 4000);
        p.SupportingFacts = p.SupportingFacts.Select(s => Truncate(s, 500)).Where(s => !string.IsNullOrWhiteSpace(s)).Take(32).ToList();
        return p;
    }

    public static RecommendationAiPayload? TryParseRecommendation(string json)
    {
        try
        {
            var p = JsonSerializer.Deserialize<RecommendationAiPayload>(json);
            return p is null ? null : SanitizeRecommendation(p);
        }
        catch
        {
            return null;
        }
    }

    public static ForecastAiPayload? TryParseForecast(string json)
    {
        try
        {
            var p = JsonSerializer.Deserialize<ForecastAiPayload>(json);
            return p is null ? null : SanitizeForecast(p);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeBand(string? band)
    {
        if (string.IsNullOrWhiteSpace(band))
            return "normal";
        if (band.Equals("optimal", StringComparison.OrdinalIgnoreCase))
            return "normal";
        return Bands.Contains(band) ? band.ToLowerInvariant() : "normal";
    }

    private static double Clamp01(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v))
            return 0;
        return Math.Clamp(v, 0, 1);
    }

    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return s.Length <= max ? s : s[..max];
    }
}
