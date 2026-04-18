using System.Text.Json;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Ai;

/// <summary>
/// Deterministic, offline-safe generator for CI and environments without API keys.
/// </summary>
public sealed class HeuristicAiInsightGenerator : IAiInsightGenerator
{
    public Task<RecommendationAiPayload> GenerateRecommendationAsync(
        string systemPrompt,
        string userJsonPayload,
        CancellationToken cancellationToken)
    {
        _ = systemPrompt;
        using var doc = System.Text.Json.JsonDocument.Parse(userJsonPayload);
        var root = doc.RootElement;
        var risk = TryGetInt(root, "current", "risk_score") ?? 0;
        var oor = TryGetLong(root, "out_of_range_active", "active_out_of_range_count") ?? 0;
        var anomalies = TryGetLong(root, "anomalies_24h", "recent_anomaly_count_24h") ?? 0;

        var priority = risk switch
        {
            >= 85 => "critical",
            >= 60 => "high",
            >= 35 => "medium",
            _ => "low",
        };

        var urgency = (oor > 0, risk) switch
        {
            (true, >= 70) => "immediate",
            (true, _) => "today",
            (_, >= 60) => "within_hours",
            _ => anomalies > 0 ? "today" : "monitor",
        };

        var scope = (oor > 0, anomalies > 0) switch
        {
            (true, true) => "anomaly_and_range",
            (true, false) => "anomaly_and_range",
            (false, true) => "anomaly_and_range",
            _ => "trend_and_state",
        };

        var confidence = Math.Min(0.9, 0.45 + risk / 200.0 + Math.Min(anomalies, 5) * 0.03);

        var title = oor > 0
            ? "Attention: readings outside expected range"
            : anomalies > 0
                ? "Recent anomaly activity"
                : "Daily soil check-in";

        var summary = BuildSummary(risk, oor, anomalies);
        var recommendation = BuildRecommendation(risk, oor);
        var probable = oor > 0
            ? "Sustained out-of-range conditions or active events reported by analytics."
            : anomalies > 0
                ? "Statistical anomaly signals were recorded in the last 24 hours."
                : "No strong anomaly or out-of-range signals in the supplied context.";

        var facts = new List<string>();
        if (risk > 0)
            facts.Add($"Risk score (analytics): {risk}");
        if (anomalies > 0)
            facts.Add($"Anomaly rows (24h window): {anomalies}");
        if (oor > 0)
            facts.Add($"Active out-of-range contexts: {oor}");
        AppendNpkThresholdFacts(root, facts);

        var payload = new RecommendationAiPayload
        {
            Priority = priority,
            Urgency = urgency,
            Confidence = confidence,
            Title = title,
            Summary = summary,
            Recommendation = recommendation,
            ProbableCause = probable,
            ReasoningScope = scope,
            SupportingFacts = facts,
        };

        return Task.FromResult(AiResponseValidator.SanitizeRecommendation(payload));
    }

    public Task<ForecastAiPayload> GenerateForecastAsync(
        string systemPrompt,
        string userJsonPayload,
        int horizonHours,
        CancellationToken cancellationToken)
    {
        _ = systemPrompt;
        using var doc = System.Text.Json.JsonDocument.Parse(userJsonPayload);
        var root = doc.RootElement;

        var temp = TryGetDouble(root, "current", "temperature");
        var moist = TryGetDouble(root, "current", "moisture");
        var cond = TryGetDouble(root, "current", "conductivity");
        var ph = TryGetDouble(root, "current", "ph_value");

        var slopeT = TryGetDouble(root, "daily_trend", "slopes", "slope_temperature");
        var slopeM = TryGetDouble(root, "daily_trend", "slopes", "slope_moisture");
        var slopeC = TryGetDouble(root, "daily_trend", "slopes", "slope_conductivity");
        var slopePh = TryGetDouble(root, "daily_trend", "slopes", "slope_ph_value");

        var factor = horizonHours / 24.0;
        var pTemp = Extrapolate(temp, slopeT, factor);
        var pMoist = Extrapolate(moist, slopeM, factor);
        var pCond = Extrapolate(cond, slopeC, factor);
        var pPh = Extrapolate(ph, slopePh, factor);

        var risk = TryGetInt(root, "current", "risk_score") ?? 0;
        var stress = risk >= 70 ? "high" : risk >= 40 ? "medium" : "low";
        var anomaly = TryGetLong(root, "anomalies_24h", "recent_anomaly_count_24h") ?? 0;
        var anomalyRisk = anomaly >= 3 ? "high" : anomaly >= 1 ? "medium" : "low";

        var payload = new ForecastAiPayload
        {
            ForecastHorizonHours = horizonHours,
            PredictedTemperature = pTemp,
            PredictedMoisture = pMoist,
            PredictedConductivity = pCond,
            PredictedPhValue = pPh,
            PredictedTemperatureBand = BandForValue(pTemp, TryGetDouble(root, "plant_thresholds", "plant_temperature_min"), TryGetDouble(root, "plant_thresholds", "plant_temperature_max")),
            PredictedMoistureBand = BandForValue(pMoist, TryGetDouble(root, "plant_thresholds", "plant_moisture_min"), TryGetDouble(root, "plant_thresholds", "plant_moisture_max")),
            PredictedConductivityBand = BandForValue(pCond, TryGetDouble(root, "plant_thresholds", "plant_conductivity_min"), TryGetDouble(root, "plant_thresholds", "plant_conductivity_max")),
            PredictedPhBand = BandForValue(pPh, TryGetDouble(root, "plant_thresholds", "plant_ph_min"), TryGetDouble(root, "plant_thresholds", "plant_ph_max")),
            PredictedRiskScore = Math.Clamp(risk + (horizonHours >= 48 ? 5 : 0), 0, 100),
            AnomalyRisk = anomalyRisk,
            StressRisk = stress,
            Confidence = Math.Min(0.85, 0.4 + (TryGetInt(root, "hourly_24h", "sample_count_24h") ?? 0) / 200.0),
            Title = $"Next {horizonHours}h outlook",
            ForecastSummary = "Short-horizon outlook based on recent trends and latest readings. Not a guarantee of future conditions.",
            RecommendedPreventiveAction = stress == "high"
                ? "Recheck irrigation and environmental stressors; confirm sensor placement."
                : "Continue monitoring; adjust care if trends worsen.",
            SupportingFacts =
            [
                "Outlook uses daily slopes and latest readings from supplied context only.",
                $"Horizon hours: {horizonHours}",
            ],
        };

        return Task.FromResult(AiResponseValidator.SanitizeForecast(payload));
    }

    private static double? Extrapolate(double? current, double? dailySlope, double factor)
    {
        if (current is null)
            return null;
        var s = dailySlope ?? 0;
        return current.Value + s * factor;
    }

    private static string BandForValue(double? value, double? min, double? max)
    {
        if (value is null)
            return "normal";
        var v = value.Value;
        if (min is not null && v < min)
            return "low";
        if (max is not null && v > max)
            return "high";
        return "normal";
    }

    private static string BuildSummary(int risk, long oor, long anomalies)
    {
        if (oor > 0 && anomalies > 0)
            return "Analytics show active out-of-range conditions and recent anomaly activity.";
        if (oor > 0)
            return "Analytics show active out-of-range conditions.";
        if (anomalies > 0)
            return "Analytics recorded anomaly activity in the last 24 hours.";
        return "Overall state looks stable based on the supplied summary signals.";
    }

    private static string BuildRecommendation(int risk, long oor)
    {
        if (oor > 0)
            return "Review irrigation, drainage, and environmental changes; verify the sensor is seated correctly and compare against plant-specific ranges.";
        if (risk >= 60)
            return "Prioritize a hands-on check today: soil feel, container drainage, and any recent weather swings.";
        return "Keep your usual monitoring cadence; no urgent action is indicated from the supplied signals.";
    }

    private static void AppendNpkThresholdFacts(JsonElement root, List<string> facts)
    {
        foreach (var (letter, key) in new[] { ("N", "npk_n"), ("P", "npk_p"), ("K", "npk_k") })
        {
            var cur = TryGetDouble(root, "current", key);
            if (cur is null)
                continue;
            var min = TryGetDouble(root, "plant_thresholds", key, "min");
            var max = TryGetDouble(root, "plant_thresholds", key, "max");
            var v = cur.Value;
            if (min is not null && v < min.Value)
                facts.Add($"Soil NPK-{letter} reading {v} below plant classifier minimum ({min}).");
            if (max is not null && v > max.Value)
                facts.Add($"Soil NPK-{letter} reading {v} above plant classifier maximum ({max}).");
        }
    }

    private static JsonElement? Navigate(JsonElement root, params string[] path)
    {
        var cur = root;
        foreach (var p in path)
        {
            if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(p, out var next))
                return null;
            cur = next;
        }
        return cur;
    }

    private static int? TryGetInt(JsonElement root, params string[] path)
    {
        var n = Navigate(root, path);
        return n?.ValueKind switch
        {
            JsonValueKind.Number when n.Value.TryGetInt32(out var i) => i,
            _ => null,
        };
    }

    private static long? TryGetLong(JsonElement root, params string[] path)
    {
        var n = Navigate(root, path);
        return n?.ValueKind switch
        {
            JsonValueKind.Number when n.Value.TryGetInt64(out var i) => i,
            _ => null,
        };
    }

    private static double? TryGetDouble(JsonElement root, params string[] path)
    {
        var n = Navigate(root, path);
        return n?.ValueKind switch
        {
            JsonValueKind.Number => n.Value.GetDouble(),
            _ => null,
        };
    }
}
