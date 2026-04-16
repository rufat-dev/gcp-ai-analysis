namespace SoilAiInsightsWorker.Models;

/// <summary>
/// One row from analytics.v_device_ai_context — keep nullable where the view can be null.
/// </summary>
public sealed class DeviceAiContextRow
{
    public string DeviceId { get; set; } = "";

    public string? UserId { get; set; }

    public string? DeviceName { get; set; }

    public string? GroupId { get; set; }

    public long? PlantType { get; set; }

    public string? PlantName { get; set; }

    public string? PlantDescription { get; set; }

    public double? PlantTemperatureMin { get; set; }

    public double? PlantTemperatureMax { get; set; }

    public double? PlantMoistureMin { get; set; }

    public double? PlantMoistureMax { get; set; }

    public double? PlantConductivityMin { get; set; }

    public double? PlantConductivityMax { get; set; }

    public double? PlantPhMin { get; set; }

    public double? PlantPhMax { get; set; }

    public double? PlantNpkMin { get; set; }

    public double? PlantNpkMax { get; set; }

    public long? RecommendedSoilType { get; set; }

    public long? SoilType { get; set; }

    public string? SoilName { get; set; }

    public string? SoilDescription { get; set; }

    public double? SandPercent { get; set; }

    public double? SiltPercent { get; set; }

    public double? BulkDensity { get; set; }

    public double? ParticleDensity { get; set; }

    public double? PorosityPercent { get; set; }

    public string? TextureClass { get; set; }

    public DateTime? ReadingTime { get; set; }

    public double? Temperature { get; set; }

    public double? Moisture { get; set; }

    public double? Conductivity { get; set; }

    public double? PhValue { get; set; }

    public long? NpkN { get; set; }

    public long? NpkP { get; set; }

    public long? NpkK { get; set; }

    public bool? TemperatureLow { get; set; }

    public bool? TemperatureHigh { get; set; }

    public bool? MoistureLow { get; set; }

    public bool? MoistureHigh { get; set; }

    public bool? ConductivityLow { get; set; }

    public bool? ConductivityHigh { get; set; }

    public bool? PhLow { get; set; }

    public bool? PhHigh { get; set; }

    public long? RiskScore { get; set; }

    public string? AiStatus { get; set; }

    public string? TemperatureTrend { get; set; }

    public string? MoistureTrend { get; set; }

    public string? ConductivityTrend { get; set; }

    public string? PhTrend { get; set; }

    public long? LastSeenMinutes { get; set; }

    public DateTime? StateUpdatedAt { get; set; }

    public DateTime? TrendDay { get; set; }

    public double? SlopeTemperature { get; set; }

    public double? SlopeMoisture { get; set; }

    public double? SlopeConductivity { get; set; }

    public double? SlopePhValue { get; set; }

    public string? TemperatureDirection { get; set; }

    public string? MoistureDirection { get; set; }

    public string? ConductivityDirection { get; set; }

    public string? PhDirection { get; set; }

    public DateTime? TrendComputedAt { get; set; }

    public long RecentAnomalyCount24h { get; set; }

    public long RecentHighAnomalyCount24h { get; set; }

    public string? TopAnomalyMetric24h { get; set; }

    public double? TopAnomalyScore24h { get; set; }

    public DateTime? LatestAnomalyTime24h { get; set; }

    /// <summary>Engineer-facing; never surface verbatim to end users.</summary>
    public string? LatestAnomalyExplanation24h { get; set; }

    public long ActiveOutOfRangeCount { get; set; }

    public long ActiveHighOutOfRangeCount { get; set; }

    public string? ActiveOutOfRangeMetric { get; set; }

    public DateTime? ActiveOutOfRangeSince { get; set; }

    public string? ActiveOutOfRangeSeverity { get; set; }

    public double? AvgTemperature24h { get; set; }

    public double? MinTemperature24h { get; set; }

    public double? MaxTemperature24h { get; set; }

    public double? AvgMoisture24h { get; set; }

    public double? MinMoisture24h { get; set; }

    public double? MaxMoisture24h { get; set; }

    public double? AvgConductivity24h { get; set; }

    public double? MinConductivity24h { get; set; }

    public double? MaxConductivity24h { get; set; }

    public double? AvgPhValue24h { get; set; }

    public double? MinPhValue24h { get; set; }

    public double? MaxPhValue24h { get; set; }

    public long? SampleCount24h { get; set; }

    public DateTime? ContextGeneratedAt { get; set; }
}
