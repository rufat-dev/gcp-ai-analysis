using System.Text.Json.Serialization;

namespace SoilAiInsightsWorker.Models;

public sealed class RecommendationAiPayload
{
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "low";

    [JsonPropertyName("urgency")]
    public string Urgency { get; set; } = "monitor";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = "";

    [JsonPropertyName("probable_cause")]
    public string ProbableCause { get; set; } = "";

    [JsonPropertyName("reasoning_scope")]
    public string ReasoningScope { get; set; } = "current_state";

    [JsonPropertyName("supporting_facts")]
    public List<string> SupportingFacts { get; set; } = [];
}

public sealed class ForecastAiPayload
{
    [JsonPropertyName("forecast_horizon_hours")]
    public int ForecastHorizonHours { get; set; }

    [JsonPropertyName("predicted_temperature")]
    public double? PredictedTemperature { get; set; }

    [JsonPropertyName("predicted_moisture")]
    public double? PredictedMoisture { get; set; }

    [JsonPropertyName("predicted_conductivity")]
    public double? PredictedConductivity { get; set; }

    [JsonPropertyName("predicted_ph_value")]
    public double? PredictedPhValue { get; set; }

    [JsonPropertyName("predicted_temperature_band")]
    public string PredictedTemperatureBand { get; set; } = "normal";

    [JsonPropertyName("predicted_moisture_band")]
    public string PredictedMoistureBand { get; set; } = "normal";

    [JsonPropertyName("predicted_conductivity_band")]
    public string PredictedConductivityBand { get; set; } = "normal";

    [JsonPropertyName("predicted_ph_band")]
    public string PredictedPhBand { get; set; } = "normal";

    [JsonPropertyName("predicted_risk_score")]
    public int PredictedRiskScore { get; set; }

    [JsonPropertyName("anomaly_risk")]
    public string AnomalyRisk { get; set; } = "low";

    [JsonPropertyName("stress_risk")]
    public string StressRisk { get; set; } = "low";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("forecast_summary")]
    public string ForecastSummary { get; set; } = "";

    [JsonPropertyName("recommended_preventive_action")]
    public string RecommendedPreventiveAction { get; set; } = "";

    [JsonPropertyName("supporting_facts")]
    public List<string> SupportingFacts { get; set; } = [];
}
