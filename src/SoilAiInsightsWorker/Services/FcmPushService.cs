using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Cloud.BigQuery.V2;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Services;

public sealed class FcmPushService : IFcmPushService
{
    private readonly BigQueryCommandRunner _bq;
    private readonly BigQueryTableResolver _resolver;
    private readonly AiInsightsWorkerOptions _options;
    private readonly PushAlertInserter _pushAlerts;
    private readonly ILogger<FcmPushService> _logger;

    public FcmPushService(
        BigQueryCommandRunner bq,
        BigQueryTableResolver resolver,
        AiInsightsWorkerOptions options,
        PushAlertInserter pushAlerts,
        ILogger<FcmPushService> logger)
    {
        _bq = bq;
        _resolver = resolver;
        _options = options;
        _pushAlerts = pushAlerts;
        _logger = logger;
    }

    public async Task TrySendUrgentRecommendationAsync(
        string? userId,
        string recommendationId,
        string deviceId,
        RecommendationAiPayload payload,
        CancellationToken cancellationToken)
    {
        if (!_options.FcmNotificationsEnabled)
            return;

        if (!RecommendationFcmEligibility.ShouldSendPush(payload))
            return;

        if (string.IsNullOrWhiteSpace(userId))
            return;

        if (!_resolver.UsersTableConfigured)
        {
            _logger.LogDebug("FCM skipped: BQ users table not configured (BQ_USERS_DATASET / BQ_USERS_TABLE).");
            return;
        }

        if (FirebaseApp.DefaultInstance is null)
        {
            _logger.LogDebug("FCM skipped: FirebaseApp not initialized (set FIREBASE_PROJECT_ID and ADC).");
            return;
        }

        var token = await LookupFcmTokenAsync(userId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogDebug("FCM skipped: no fcm_token for user_id {UserId}", userId);
            return;
        }

        var fcmTitle = Truncate(payload.Title, 120);
        var dbTitle = Truncate(payload.Title, 500);
        var body = Truncate(payload.Summary, 240);
        var alertMessage = BuildCrmAlertMessage(body, payload);
        var message = new Message
        {
            Token = token,
            Notification = new Notification { Title = fcmTitle, Body = body },
            Data = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["type"] = "ai_recommendation",
                ["recommendation_id"] = recommendationId,
                ["device_id"] = deviceId,
                ["priority"] = payload.Priority,
                ["urgency"] = payload.Urgency,
            },
        };

        try
        {
            await FirebaseMessaging.DefaultInstance
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(false);
            _logger.LogInformation(
                "FCM sent urgent recommendation {RecommendationId} to user {UserId}",
                recommendationId,
                userId);
            await _pushAlerts
                .TryMergeInsertAfterPushAsync(
                    userId,
                    deviceId,
                    recommendationId,
                    dbTitle,
                    alertMessage,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(
                ex,
                "FCM send failed for user {UserId} recommendation {RecommendationId}: {MessagingError}",
                userId,
                recommendationId,
                ex.MessagingErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "FCM send failed for user {UserId} recommendation {RecommendationId}",
                userId,
                recommendationId);
        }
    }

    private async Task<string?> LookupFcmTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var qualified = $"`{_resolver.ProjectId}.{_resolver.UsersDataset}.{_resolver.UsersTable}`";
        var sql = $"""
                   SELECT fcm_token
                   FROM {qualified}
                   WHERE user_id = @user_id
                   LIMIT 1
                   """;

        var rows = await _bq.ExecuteQueryAsync(
                sql,
                [new BigQueryParameter("user_id", BigQueryDbType.String, userId)],
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var row in rows)
        {
            if (row["fcm_token"] is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }

        return null;
    }

    private static string Truncate(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return value.Length <= maxLen ? value : value[..maxLen];
    }

    /// <summary>Full in-app / CRM body (BigQuery <c>message</c>, max 8000).</summary>
    private static string BuildCrmAlertMessage(string summaryLine, RecommendationAiPayload payload)
    {
        const int maxLen = 8000;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(summaryLine))
            parts.Add(summaryLine.Trim());
        if (!string.IsNullOrWhiteSpace(payload.Recommendation))
            parts.Add(payload.Recommendation.Trim());
        var combined = string.Join("\n\n", parts);
        return combined.Length <= maxLen ? combined : combined[..maxLen];
    }
}
