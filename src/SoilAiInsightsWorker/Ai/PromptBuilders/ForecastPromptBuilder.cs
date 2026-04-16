namespace SoilAiInsightsWorker.Ai.PromptBuilders;

public static class ForecastPromptBuilder
{
    public static string SystemPrompt(string promptVersion)
    {
        return $"""
You produce short-horizon outlook JSON for soil monitoring (not scientific certainty).
Rules:
- Use ONLY facts present in the JSON context. Do not invent measurements.
- Anomaly detection is upstream; phrase risks as possibilities, not certainties.
- Bands must be one of: low|normal|high (map prior 'optimal' mentally to 'normal' if needed).
- predicted_* numeric fields may be null only if context is insufficient; prefer conservative estimates from recent hourly stats when possible.
- Output MUST be a single JSON object matching the schema exactly (no markdown).
- anomaly_risk and stress_risk: low|medium|high

Prompt version: {promptVersion}
""";
    }
}
