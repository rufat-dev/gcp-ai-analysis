namespace SoilAiInsightsWorker.Models;

public sealed class AiInsightsWorkerOptions
{
    public const string SectionName = "AiInsightsWorker";

    /// <summary>
    /// IANA timezone id for logical insight day (e.g. Europe/Berlin). Empty = UTC.
    /// Environment override: <c>AI_INSIGHTS_TIMEZONE</c> or <c>AiInsightsWorker__InsightTimeZone</c>.
    /// </summary>
    public string InsightTimeZone { get; set; } = "";

    public string PromptVersion { get; set; } = "dev";

    /// <summary>Logical model label stored in BigQuery (OpenAI model or heuristic id).</summary>
    public string ModelName { get; set; } = "heuristic-v1";

    public int[] ForecastHorizonHours { get; set; } = [24, 48];

    public int MaxConcurrentDevices { get; set; } = 4;

    public bool SkipDeviceIfNoReadings { get; set; } = true;

    /// <summary>Skip if last_seen_minutes exceeds this (default 7 days). Null disables check.</summary>
    public int? MaxLastSeenMinutesAllowed { get; set; } = 10080;

    /// <summary>Heuristic | OpenAi — OpenAi requires env OPENAI_API_KEY.</summary>
    public string AiProvider { get; set; } = "Heuristic";

    public string OpenAiApiBaseUrl { get; set; } = "https://api.openai.com/v1/";

    public string OpenAiChatModel { get; set; } = "gpt-4o-mini";

    public int OpenAiTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// When true (default), sends FCM for high/critical + urgent recommendations if <c>fcm_token</c> exists.
    /// Disable with config or env <c>AI_INSIGHTS_FCM_ENABLED=false</c>.
    /// </summary>
    public bool FcmNotificationsEnabled { get; set; } = true;

    /// <summary>When true (default), inserts a row into <c>crm.alerts</c> after a successful FCM send.</summary>
    public bool PersistAlertsAfterFcmPush { get; set; } = true;

}

public enum AiInsightsRunMode
{
    Full,
    RecommendationsOnly,
    ForecastsOnly
}

public sealed class AiInsightsRunRequest
{
    /// <summary>Optional override for logical insight day (UTC date used with timezone).</summary>
    public DateTime? InsightDateUtc { get; set; }

    public AiInsightsRunMode Mode { get; set; } = AiInsightsRunMode.Full;
}

public sealed class AiInsightsRunResponse
{
    public string Status { get; set; } = "ok";

    public DateTime InsightDateUtc { get; set; }

    public int DevicesScanned { get; set; }

    public int DevicesSkipped { get; set; }

    public int RecommendationRowsWritten { get; set; }

    public int ForecastRowsWritten { get; set; }

    public int ProviderFailures { get; set; }

    public int ParseFailures { get; set; }

    public int MergeFailures { get; set; }

    public List<string> Notes { get; } = [];
}
