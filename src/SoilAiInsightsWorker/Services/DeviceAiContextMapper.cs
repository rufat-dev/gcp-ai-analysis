using Google.Cloud.BigQuery.V2;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Services;

public static class DeviceAiContextMapper
{
    public static DeviceAiContextRow Map(BigQueryRow row)
    {
        return new DeviceAiContextRow
        {
            DeviceId = GetString(row, "device_id") ?? "",
            UserId = GetString(row, "user_id"),
            DeviceName = GetString(row, "device_name"),
            GroupId = GetString(row, "group_id"),
            PlantType = GetLong(row, "plant_type"),
            PlantName = GetString(row, "plant_name"),
            PlantDescription = GetString(row, "plant_description"),
            PlantTemperatureMin = GetDouble(row, "plant_temperature_min"),
            PlantTemperatureMax = GetDouble(row, "plant_temperature_max"),
            PlantMoistureMin = GetDouble(row, "plant_moisture_min"),
            PlantMoistureMax = GetDouble(row, "plant_moisture_max"),
            PlantConductivityMin = GetDouble(row, "plant_conductivity_min"),
            PlantConductivityMax = GetDouble(row, "plant_conductivity_max"),
            PlantPhMin = GetDouble(row, "plant_ph_min"),
            PlantPhMax = GetDouble(row, "plant_ph_max"),
            PlantNpkNMin = GetDouble(row, "plant_npk_n_min"),
            PlantNpkNMax = GetDouble(row, "plant_npk_n_max"),
            PlantNpkPMin = GetDouble(row, "plant_npk_p_min"),
            PlantNpkPMax = GetDouble(row, "plant_npk_p_max"),
            PlantNpkKMin = GetDouble(row, "plant_npk_k_min"),
            PlantNpkKMax = GetDouble(row, "plant_npk_k_max"),
            RecommendedSoilType = GetLong(row, "recommended_soil_type"),
            SoilType = GetLong(row, "soil_type"),
            SoilName = GetString(row, "soil_name"),
            SoilDescription = GetString(row, "soil_description"),
            SandPercent = GetDouble(row, "sand_percent"),
            SiltPercent = GetDouble(row, "silt_percent"),
            BulkDensity = GetDouble(row, "bulk_density"),
            ParticleDensity = GetDouble(row, "particle_density"),
            PorosityPercent = GetDouble(row, "porosity_percent"),
            TextureClass = GetString(row, "texture_class"),
            ReadingTime = GetDateTime(row, "reading_time"),
            Temperature = GetDouble(row, "temperature"),
            Moisture = GetDouble(row, "moisture"),
            Conductivity = GetDouble(row, "conductivity"),
            PhValue = GetDouble(row, "ph_value"),
            NpkN = GetLong(row, "npk_n"),
            NpkP = GetLong(row, "npk_p"),
            NpkK = GetLong(row, "npk_k"),
            TemperatureLow = GetBool(row, "temperature_low"),
            TemperatureHigh = GetBool(row, "temperature_high"),
            MoistureLow = GetBool(row, "moisture_low"),
            MoistureHigh = GetBool(row, "moisture_high"),
            ConductivityLow = GetBool(row, "conductivity_low"),
            ConductivityHigh = GetBool(row, "conductivity_high"),
            PhLow = GetBool(row, "ph_low"),
            PhHigh = GetBool(row, "ph_high"),
            RiskScore = GetLong(row, "risk_score"),
            AiStatus = GetString(row, "ai_status"),
            TemperatureTrend = GetString(row, "temperature_trend"),
            MoistureTrend = GetString(row, "moisture_trend"),
            ConductivityTrend = GetString(row, "conductivity_trend"),
            PhTrend = GetString(row, "ph_trend"),
            LastSeenMinutes = GetLong(row, "last_seen_minutes"),
            StateUpdatedAt = GetDateTime(row, "state_updated_at"),
            TrendDay = GetDateTime(row, "trend_day"),
            SlopeTemperature = GetDouble(row, "slope_temperature"),
            SlopeMoisture = GetDouble(row, "slope_moisture"),
            SlopeConductivity = GetDouble(row, "slope_conductivity"),
            SlopePhValue = GetDouble(row, "slope_ph_value"),
            TemperatureDirection = GetString(row, "temperature_direction"),
            MoistureDirection = GetString(row, "moisture_direction"),
            ConductivityDirection = GetString(row, "conductivity_direction"),
            PhDirection = GetString(row, "ph_direction"),
            TrendComputedAt = GetDateTime(row, "trend_computed_at"),
            RecentAnomalyCount24h = GetLong(row, "recent_anomaly_count_24h") ?? 0,
            RecentHighAnomalyCount24h = GetLong(row, "recent_high_anomaly_count_24h") ?? 0,
            TopAnomalyMetric24h = GetString(row, "top_anomaly_metric_24h"),
            TopAnomalyScore24h = GetDouble(row, "top_anomaly_score_24h"),
            LatestAnomalyTime24h = GetDateTime(row, "latest_anomaly_time_24h"),
            LatestAnomalyExplanation24h = GetString(row, "latest_anomaly_explanation_24h"),
            ActiveOutOfRangeCount = GetLong(row, "active_out_of_range_count") ?? 0,
            ActiveHighOutOfRangeCount = GetLong(row, "active_high_out_of_range_count") ?? 0,
            ActiveOutOfRangeMetric = GetString(row, "active_out_of_range_metric"),
            ActiveOutOfRangeSince = GetDateTime(row, "active_out_of_range_since"),
            ActiveOutOfRangeSeverity = GetString(row, "active_out_of_range_severity"),
            AvgTemperature24h = GetDouble(row, "avg_temperature_24h"),
            MinTemperature24h = GetDouble(row, "min_temperature_24h"),
            MaxTemperature24h = GetDouble(row, "max_temperature_24h"),
            AvgMoisture24h = GetDouble(row, "avg_moisture_24h"),
            MinMoisture24h = GetDouble(row, "min_moisture_24h"),
            MaxMoisture24h = GetDouble(row, "max_moisture_24h"),
            AvgConductivity24h = GetDouble(row, "avg_conductivity_24h"),
            MinConductivity24h = GetDouble(row, "min_conductivity_24h"),
            MaxConductivity24h = GetDouble(row, "max_conductivity_24h"),
            AvgPhValue24h = GetDouble(row, "avg_ph_value_24h"),
            MinPhValue24h = GetDouble(row, "min_ph_value_24h"),
            MaxPhValue24h = GetDouble(row, "max_ph_value_24h"),
            SampleCount24h = GetLong(row, "sample_count_24h"),
            ContextGeneratedAt = GetDateTime(row, "context_generated_at"),
        };
    }

    private static string? GetString(BigQueryRow row, string name)
    {
        if (!row.Schema.Fields.Any(f => f.Name == name))
            return null;
        return row[name] is null or DBNull ? null : Convert.ToString(row[name], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static long? GetLong(BigQueryRow row, string name)
    {
        if (!row.Schema.Fields.Any(f => f.Name == name))
            return null;
        var v = row[name];
        if (v is null or DBNull)
            return null;
        return Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static double? GetDouble(BigQueryRow row, string name)
    {
        if (!row.Schema.Fields.Any(f => f.Name == name))
            return null;
        var v = row[name];
        if (v is null or DBNull)
            return null;
        return Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool? GetBool(BigQueryRow row, string name)
    {
        if (!row.Schema.Fields.Any(f => f.Name == name))
            return null;
        var v = row[name];
        if (v is null or DBNull)
            return null;
        return Convert.ToBoolean(v, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static DateTime? GetDateTime(BigQueryRow row, string name)
    {
        if (!row.Schema.Fields.Any(f => f.Name == name))
            return null;
        var v = row[name];
        if (v is null or DBNull)
            return null;
        if (v is DateTime dt)
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return Convert.ToDateTime(v, System.Globalization.CultureInfo.InvariantCulture);
    }
}
