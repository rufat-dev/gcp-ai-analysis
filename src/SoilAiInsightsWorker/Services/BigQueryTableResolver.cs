namespace SoilAiInsightsWorker.Services;

/// <summary>
/// Resolves BigQuery resource names from environment variables (mirrors analytics worker pattern).
/// </summary>
public sealed class BigQueryTableResolver
{
    public BigQueryTableResolver()
    {
        ProjectId = Required("BQ_PROJECT_ID");
        AnalyticsDataset = Optional("BQ_ANALYTICS_DATASET") ?? "analytics";
        AiContextViewName = Optional("BQ_AI_CONTEXT_VIEW") ?? "v_device_ai_context";
        AiRecommendationsTable = Optional("BQ_AI_RECOMMENDATIONS_TABLE") ?? "ai_recommendations";
        AiForecastsTable = Optional("BQ_AI_FORECASTS_TABLE") ?? "ai_forecasts";
        UsersDataset = Optional("BQ_USERS_DATASET");
        UsersTable = Optional("BQ_USERS_TABLE");
        AlertsDataset = Optional("BQ_ALERTS_DATASET") ?? "crm";
        AlertsTable = Optional("BQ_ALERTS_TABLE") ?? "alerts";
    }

    public string ProjectId { get; }

    public string AnalyticsDataset { get; }

    public string AiContextViewName { get; }

    public string AiRecommendationsTable { get; }

    public string AiForecastsTable { get; }

    public string FullyQualifiedContextView =>
        $"`{ProjectId}.{AnalyticsDataset}.{AiContextViewName}`";

    public string FullyQualifiedRecommendationsTable =>
        $"`{ProjectId}.{AnalyticsDataset}.{AiRecommendationsTable}`";

    public string FullyQualifiedForecastsTable =>
        $"`{ProjectId}.{AnalyticsDataset}.{AiForecastsTable}`";

    /// <summary>Optional; same env vars as SoilReportFn. Required for FCM token lookup.</summary>
    public string? UsersDataset { get; }

    /// <summary>Optional; same env vars as SoilReportFn. Required for FCM token lookup.</summary>
    public string? UsersTable { get; }

    public bool UsersTableConfigured =>
        !string.IsNullOrWhiteSpace(UsersDataset) && !string.IsNullOrWhiteSpace(UsersTable);

    public string AlertsDataset { get; }

    public string AlertsTable { get; }

    public string FullyQualifiedAlertsTable =>
        $"`{ProjectId}.{AlertsDataset}.{AlertsTable}`";

    private static string Required(string name)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(v))
            throw new InvalidOperationException($"Required environment variable '{name}' is not set.");
        return v.Trim();
    }

    private static string? Optional(string name) =>
        Environment.GetEnvironmentVariable(name)?.Trim();
}
