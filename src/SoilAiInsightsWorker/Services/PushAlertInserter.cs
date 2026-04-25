using Google.Cloud.BigQuery.V2;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Services;

/// <summary>
/// Persists a CRM alert after a successful FCM send (same table as SoilReportFn GET /alerts).
/// </summary>
public sealed class PushAlertInserter
{
    private readonly BigQueryCommandRunner _bq;
    private readonly BigQueryTableResolver _resolver;
    private readonly AiInsightsWorkerOptions _options;
    private readonly ILogger<PushAlertInserter> _logger;

    public PushAlertInserter(
        BigQueryCommandRunner bq,
        BigQueryTableResolver resolver,
        AiInsightsWorkerOptions options,
        ILogger<PushAlertInserter> logger)
    {
        _bq = bq;
        _resolver = resolver;
        _options = options;
        _logger = logger;
    }

    /// <summary>Deterministic id so re-runs of the job do not duplicate rows.</summary>
    public static string AlertIdForRecommendationPush(string recommendationId) =>
        $"aimsg_{recommendationId}";

    public async Task TryMergeInsertAfterPushAsync(
        string userId,
        string deviceId,
        string recommendationId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        if (!_options.PersistAlertsAfterFcmPush)
            return;

        if (string.IsNullOrWhiteSpace(userId))
            return;

        var qualified = _resolver.FullyQualifiedAlertsTable;
        var alertId = AlertIdForRecommendationPush(recommendationId);
        var now = DateTime.UtcNow;

        var sql = $"""
                   MERGE {qualified} AS T
                   USING (
                     SELECT
                       @alert_id AS alert_id,
                       @device_id AS device_id,
                       @user_id AS user_id,
                       CAST(NULL AS STRING) AS device_group_id,
                       @is_read AS is_read,
                       @title AS title,
                       @message AS message,
                       @created_at AS created_at,
                       @isDeleted AS isDeleted
                   ) AS S
                   ON T.alert_id = S.alert_id
                   WHEN NOT MATCHED THEN
                     INSERT (alert_id, device_id, user_id, device_group_id, is_read, title, message, created_at, isDeleted)
                     VALUES (S.alert_id, S.device_id, S.user_id, S.device_group_id, S.is_read, S.title, S.message, S.created_at, S.isDeleted)
                   """;

        var parameters = new List<BigQueryParameter>
        {
            new("alert_id", BigQueryDbType.String, alertId),
            new("device_id", BigQueryDbType.String, deviceId),
            new("user_id", BigQueryDbType.String, userId),
            new("is_read", BigQueryDbType.Bool, false),
            new("title", BigQueryDbType.String, string.IsNullOrWhiteSpace(title) ? "Soil alert" : title.Trim()),
            new("message", BigQueryDbType.String, message.Trim()),
            new("created_at", BigQueryDbType.Timestamp, now),
            new("isDeleted", BigQueryDbType.Bool, false),
        };

        try
        {
            await _bq.ExecuteDmlAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("CRM alert {AlertId} saved for user {UserId}", alertId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save CRM alert {AlertId} for user {UserId}", alertId, userId);
        }
    }
}
