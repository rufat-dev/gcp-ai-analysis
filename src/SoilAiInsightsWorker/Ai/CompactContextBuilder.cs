using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Ai;

/// <summary>
/// Builds compact JSON for model prompts — excludes raw anomaly explanation text from user-facing copy.
/// </summary>
public static class CompactContextBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static string BuildRecommendationPayload(DeviceAiContextRow c, DateTime insightDateStartUtc)
    {
        var payload = new
        {
            insight_date_utc = insightDateStartUtc,
            device = new { c.DeviceId, c.DeviceName, c.UserId, plant = c.PlantName, soil = c.SoilName },
            current = new
            {
                c.ReadingTime,
                c.Temperature,
                c.Moisture,
                c.Conductivity,
                c.PhValue,
                c.NpkN,
                c.NpkP,
                c.NpkK,
                flags = new
                {
                    c.TemperatureLow,
                    c.TemperatureHigh,
                    c.MoistureLow,
                    c.MoistureHigh,
                    c.ConductivityLow,
                    c.ConductivityHigh,
                    c.PhLow,
                    c.PhHigh,
                },
                c.RiskScore,
                c.AiStatus,
                trends = new { c.TemperatureTrend, c.MoistureTrend, c.ConductivityTrend, c.PhTrend },
                c.LastSeenMinutes,
            },
            plant_thresholds = new
            {
                c.PlantTemperatureMin,
                c.PlantTemperatureMax,
                c.PlantMoistureMin,
                c.PlantMoistureMax,
                c.PlantConductivityMin,
                c.PlantConductivityMax,
                c.PlantPhMin,
                c.PlantPhMax,
                npk_n = new { min = c.PlantNpkNMin, max = c.PlantNpkNMax },
                npk_p = new { min = c.PlantNpkPMin, max = c.PlantNpkPMax },
                npk_k = new { min = c.PlantNpkKMin, max = c.PlantNpkKMax },
            },
            soil = new
            {
                c.TextureClass,
                c.PorosityPercent,
                c.SandPercent,
                c.SiltPercent,
                c.BulkDensity,
            },
            daily_trend = new
            {
                c.TrendDay,
                slopes = new { c.SlopeTemperature, c.SlopeMoisture, c.SlopeConductivity, c.SlopePhValue },
                directions = new { c.TemperatureDirection, c.MoistureDirection, c.ConductivityDirection, c.PhDirection },
            },
            anomalies_24h = new
            {
                c.RecentAnomalyCount24h,
                c.RecentHighAnomalyCount24h,
                top_metric = c.TopAnomalyMetric24h,
                top_score = c.TopAnomalyScore24h,
                latest_time = c.LatestAnomalyTime24h,
            },
            out_of_range_active = new
            {
                c.ActiveOutOfRangeCount,
                c.ActiveHighOutOfRangeCount,
                primary_metric = c.ActiveOutOfRangeMetric,
                since = c.ActiveOutOfRangeSince,
                severity = c.ActiveOutOfRangeSeverity,
            },
            hourly_24h = new
            {
                c.AvgTemperature24h,
                c.MinTemperature24h,
                c.MaxTemperature24h,
                c.AvgMoisture24h,
                c.MinMoisture24h,
                c.MaxMoisture24h,
                c.AvgConductivity24h,
                c.MinConductivity24h,
                c.MaxConductivity24h,
                c.AvgPhValue24h,
                c.MinPhValue24h,
                c.MaxPhValue24h,
                c.SampleCount24h,
            },
            context_generated_at = c.ContextGeneratedAt,
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildForecastPayload(DeviceAiContextRow c, DateTime insightDateStartUtc, int horizonHours)
    {
        var root = JsonNode.Parse(BuildRecommendationPayload(c, insightDateStartUtc))!.AsObject();
        root["forecast_horizon_hours"] = horizonHours;
        return root.ToJsonString(JsonOptions);
    }

    /// <summary>Structured internal context for raw_context column (includes non-UI anomaly metadata).</summary>
    public static string BuildRawContextJson(DeviceAiContextRow c, DateTime insightDateStartUtc, string stage)
    {
        var payload = new
        {
            stage,
            insight_date_utc = insightDateStartUtc,
            c.DeviceId,
            engineer_anomaly_hint = new
            {
                c.TopAnomalyMetric24h,
                c.TopAnomalyScore24h,
                has_explanation = !string.IsNullOrEmpty(c.LatestAnomalyExplanation24h),
            },
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildTriggeredByJson(DeviceAiContextRow c)
    {
        var o = new
        {
            risk_score = c.RiskScore,
            ai_status = c.AiStatus,
            recent_anomaly_count_24h = c.RecentAnomalyCount24h,
            active_out_of_range_count = c.ActiveOutOfRangeCount,
            flags = new
            {
                c.TemperatureLow,
                c.TemperatureHigh,
                c.MoistureLow,
                c.MoistureHigh,
            },
        };
        return JsonSerializer.Serialize(o, JsonOptions);
    }
}
