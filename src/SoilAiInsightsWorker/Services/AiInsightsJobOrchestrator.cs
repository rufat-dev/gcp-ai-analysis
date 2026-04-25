using System.Collections.Concurrent;
using Google.Cloud.BigQuery.V2;
using SoilAiInsightsWorker.Ai;
using SoilAiInsightsWorker.Ai.PromptBuilders;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Services;

public sealed class AiInsightsJobOrchestrator
{
    private readonly BigQueryCommandRunner _bq;
    private readonly BigQueryTableResolver _resolver;
    private readonly SqlTemplateProvider _sql;
    private readonly AiInsightsWorkerOptions _options;
    private readonly IAiInsightGenerator _ai;
    private readonly IFcmPushService _fcm;
    private readonly ILogger<AiInsightsJobOrchestrator> _logger;

    public AiInsightsJobOrchestrator(
        BigQueryCommandRunner bq,
        BigQueryTableResolver resolver,
        SqlTemplateProvider sql,
        AiInsightsWorkerOptions options,
        IAiInsightGenerator ai,
        IFcmPushService fcm,
        ILogger<AiInsightsJobOrchestrator> logger)
    {
        _bq = bq;
        _resolver = resolver;
        _sql = sql;
        _options = options;
        _ai = ai;
        _fcm = fcm;
        _logger = logger;
    }

    public async Task<AiInsightsRunResponse> RunAsync(
        AiInsightsRunRequest request,
        string? deviceIdFilter,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var insightStart = request.InsightDateUtc
            ?? InsightScheduleHelper.GetInsightDateStartUtc(utcNow, _options.InsightTimeZone);

        var response = new AiInsightsRunResponse
        {
            InsightDateUtc = insightStart,
        };

        var template = _sql.LoadTemplate("select_ai_context.sql");
        var deviceFilter = string.IsNullOrWhiteSpace(deviceIdFilter)
            ? ""
            : "AND device_id = @device_id";
        var sqlText = SqlTemplateProvider.Apply(template, new Dictionary<string, string>
        {
            ["PROJECT"] = _resolver.ProjectId,
            ["ANALYTICS_DATASET"] = _resolver.AnalyticsDataset,
            ["DEVICE_FILTER"] = deviceFilter,
        });

        var parameters = new List<BigQueryParameter>();
        if (!string.IsNullOrWhiteSpace(deviceIdFilter))
            parameters.Add(new BigQueryParameter("device_id", BigQueryDbType.String, deviceIdFilter));

        var results = await _bq.ExecuteQueryAsync(sqlText, parameters, cancellationToken).ConfigureAwait(false);
        var rows = new List<DeviceAiContextRow>();
        foreach (var row in results)
            rows.Add(DeviceAiContextMapper.Map(row));

        response.DevicesScanned = rows.Count;

        var mergeRecSql = SqlTemplateProvider.Apply(_sql.LoadTemplate("merge_ai_recommendations.sql"), new Dictionary<string, string>
        {
            ["PROJECT"] = _resolver.ProjectId,
            ["ANALYTICS_DATASET"] = _resolver.AnalyticsDataset,
            ["RECOMMENDATIONS_TABLE"] = _resolver.AiRecommendationsTable,
        });

        var mergeFcSql = SqlTemplateProvider.Apply(_sql.LoadTemplate("merge_ai_forecasts.sql"), new Dictionary<string, string>
        {
            ["PROJECT"] = _resolver.ProjectId,
            ["ANALYTICS_DATASET"] = _resolver.AnalyticsDataset,
            ["FORECASTS_TABLE"] = _resolver.AiForecastsTable,
        });

        var sem = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentDevices));
        var recCount = new ConcurrentCounter();
        var fcCount = new ConcurrentCounter();
        var skipped = new ConcurrentCounter();
        var providerFail = new ConcurrentCounter();
        var parseFail = new ConcurrentCounter();
        var mergeFail = new ConcurrentCounter();

        var horizons = _options.ForecastHorizonHours is { Length: > 0 } h ? h : new[] { 24, 48 };

        var tasks = rows.Select(device => Task.Run(async () =>
        {
            var d = device;
            await sem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (ShouldSkip(d, out var reason))
                {
                    skipped.Increment();
                    _logger.LogInformation("Skip device {DeviceId}: {Reason}", d.DeviceId, reason);
                    return;
                }

                var sysRec = RecommendationPromptBuilder.SystemPrompt(_options.PromptVersion);
                var sysFc = ForecastPromptBuilder.SystemPrompt(_options.PromptVersion);
                var userRec = CompactContextBuilder.BuildRecommendationPayload(d, insightStart);

                RecommendationAiPayload? recPayload = null;

                if (request.Mode is AiInsightsRunMode.Full or AiInsightsRunMode.RecommendationsOnly)
                {
                    try
                    {
                        recPayload = await _ai.GenerateRecommendationAsync(sysRec, userRec, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        providerFail.Increment();
                        _logger.LogWarning(ex, "Recommendation provider failure for {DeviceId}", d.DeviceId);
                    }

                    if (recPayload is null)
                    {
                        parseFail.Increment();
                    }
                    else
                    {
                        try
                        {
                            var recommendationId = DeterministicIds.RecommendationId(d.DeviceId, insightStart);
                            await MergeRecommendationAsync(
                                    mergeRecSql,
                                    d,
                                    insightStart,
                                    recommendationId,
                                    recPayload,
                                    cancellationToken)
                                .ConfigureAwait(false);
                            await _fcm
                                .TrySendUrgentRecommendationAsync(
                                    d.UserId,
                                    recommendationId,
                                    d.DeviceId,
                                    recPayload,
                                    cancellationToken)
                                .ConfigureAwait(false);
                            recCount.Increment();
                        }
                        catch (Exception ex)
                        {
                            mergeFail.Increment();
                            _logger.LogWarning(ex, "Recommendation merge failure for {DeviceId}", d.DeviceId);
                        }
                    }
                }

                if (request.Mode is AiInsightsRunMode.Full or AiInsightsRunMode.ForecastsOnly)
                {
                    foreach (var horizon in horizons)
                    {
                        ForecastAiPayload? fc = null;
                        try
                        {
                            var userFc = CompactContextBuilder.BuildForecastPayload(d, insightStart, horizon);
                            fc = await _ai.GenerateForecastAsync(sysFc, userFc, horizon, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            providerFail.Increment();
                            _logger.LogWarning(ex, "Forecast provider failure for {DeviceId} horizon {H}h", d.DeviceId, horizon);
                        }

                        if (fc is null)
                        {
                            parseFail.Increment();
                            continue;
                        }

                        try
                        {
                            await MergeForecastAsync(mergeFcSql, d, insightStart, fc, cancellationToken).ConfigureAwait(false);
                            fcCount.Increment();
                        }
                        catch (Exception ex)
                        {
                            mergeFail.Increment();
                            _logger.LogWarning(ex, "Forecast merge failure for {DeviceId} {H}h", d.DeviceId, horizon);
                        }
                    }
                }
            }
            finally
            {
                sem.Release();
            }
        }, cancellationToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);

        response.DevicesSkipped = skipped.Value;
        response.RecommendationRowsWritten = recCount.Value;
        response.ForecastRowsWritten = fcCount.Value;
        response.ProviderFailures = providerFail.Value;
        response.ParseFailures = parseFail.Value;
        response.MergeFailures = mergeFail.Value;

        return response;
    }

    private bool ShouldSkip(DeviceAiContextRow d, out string reason)
    {
        if (string.IsNullOrWhiteSpace(d.DeviceId))
        {
            reason = "empty device_id";
            return true;
        }

        if (_options.SkipDeviceIfNoReadings
            && d.ReadingTime is null
            && d.Temperature is null
            && d.Moisture is null)
        {
            reason = "no readings in context";
            return true;
        }

        if (_options.MaxLastSeenMinutesAllowed is { } max && d.LastSeenMinutes is { } ls && ls > max)
        {
            reason = $"stale last_seen_minutes ({ls} > {max})";
            return true;
        }

        reason = "";
        return false;
    }

    private async Task MergeRecommendationAsync(
        string mergeSql,
        DeviceAiContextRow d,
        DateTime insightStartUtc,
        string recommendationId,
        RecommendationAiPayload p,
        CancellationToken cancellationToken)
    {
        var id = recommendationId;
        var now = DateTime.UtcNow;
        var supportingFactsJson = System.Text.Json.JsonSerializer.Serialize(p.SupportingFacts);
        var triggeredBy = CompactContextBuilder.BuildTriggeredByJson(d);
        var rawContext = CompactContextBuilder.BuildRawContextJson(d, insightStartUtc, "recommendation");
        var modelOutput = System.Text.Json.JsonSerializer.Serialize(p);

        var parameters = new List<BigQueryParameter>
        {
            new("recommendation_id", BigQueryDbType.String, id),
            new("device_id", BigQueryDbType.String, d.DeviceId),
            new("user_id", BigQueryDbType.String, d.UserId ?? (object)DBNull.Value),
            new("insight_date", BigQueryDbType.DateTime, insightStartUtc),
            new("status", BigQueryDbType.String, "active"),
            new("priority", BigQueryDbType.String, p.Priority),
            new("urgency", BigQueryDbType.String, p.Urgency),
            new("confidence", BigQueryDbType.Float64, p.Confidence),
            new("title", BigQueryDbType.String, p.Title),
            new("summary", BigQueryDbType.String, p.Summary),
            new("recommendation", BigQueryDbType.String, p.Recommendation),
            new("probable_cause", BigQueryDbType.String, p.ProbableCause),
            new("reasoning_scope", BigQueryDbType.String, p.ReasoningScope),
            new("risk_score", BigQueryDbType.Int64, d.RiskScore ?? (object)DBNull.Value),
            new("ai_status", BigQueryDbType.String, d.AiStatus ?? (object)DBNull.Value),
            new("active_anomaly_count", BigQueryDbType.Int64, d.RecentAnomalyCount24h),
            new("active_out_of_range_count", BigQueryDbType.Int64, d.ActiveOutOfRangeCount),
            new("supporting_facts_json", BigQueryDbType.String, supportingFactsJson),
            new("triggered_by_json", BigQueryDbType.String, triggeredBy),
            new("raw_context_json", BigQueryDbType.String, rawContext),
            new("model_output_json", BigQueryDbType.String, modelOutput),
            new("model_name", BigQueryDbType.String, _options.ModelName),
            new("prompt_version", BigQueryDbType.String, _options.PromptVersion),
            new("created_at", BigQueryDbType.Timestamp, now),
            new("updated_at", BigQueryDbType.Timestamp, now),
        };

        await _bq.ExecuteDmlAsync(mergeSql, parameters, cancellationToken).ConfigureAwait(false);
    }

    private async Task MergeForecastAsync(
        string mergeSql,
        DeviceAiContextRow d,
        DateTime insightStartUtc,
        ForecastAiPayload p,
        CancellationToken cancellationToken)
    {
        var id = DeterministicIds.ForecastId(d.DeviceId, insightStartUtc, p.ForecastHorizonHours);
        var now = DateTime.UtcNow;
        var forecastFor = DateTime.SpecifyKind(insightStartUtc.AddHours(p.ForecastHorizonHours), DateTimeKind.Utc);
        var supportingFactsJson = System.Text.Json.JsonSerializer.Serialize(p.SupportingFacts);
        var rawContext = CompactContextBuilder.BuildRawContextJson(d, insightStartUtc, $"forecast_{p.ForecastHorizonHours}h");
        var modelOutput = System.Text.Json.JsonSerializer.Serialize(p);

        var parameters = new List<BigQueryParameter>
        {
            new("forecast_id", BigQueryDbType.String, id),
            new("device_id", BigQueryDbType.String, d.DeviceId),
            new("user_id", BigQueryDbType.String, d.UserId ?? (object)DBNull.Value),
            new("forecast_date", BigQueryDbType.DateTime, insightStartUtc),
            new("forecast_horizon_hours", BigQueryDbType.Int64, p.ForecastHorizonHours),
            new("forecast_for_time", BigQueryDbType.Timestamp, forecastFor),
            new("predicted_temperature", BigQueryDbType.Float64, p.PredictedTemperature ?? (object)DBNull.Value),
            new("predicted_moisture", BigQueryDbType.Float64, p.PredictedMoisture ?? (object)DBNull.Value),
            new("predicted_conductivity", BigQueryDbType.Float64, p.PredictedConductivity ?? (object)DBNull.Value),
            new("predicted_ph_value", BigQueryDbType.Float64, p.PredictedPhValue ?? (object)DBNull.Value),
            new("predicted_temperature_band", BigQueryDbType.String, p.PredictedTemperatureBand),
            new("predicted_moisture_band", BigQueryDbType.String, p.PredictedMoistureBand),
            new("predicted_conductivity_band", BigQueryDbType.String, p.PredictedConductivityBand),
            new("predicted_ph_band", BigQueryDbType.String, p.PredictedPhBand),
            new("predicted_risk_score", BigQueryDbType.Int64, p.PredictedRiskScore),
            new("anomaly_risk", BigQueryDbType.String, p.AnomalyRisk),
            new("stress_risk", BigQueryDbType.String, p.StressRisk),
            new("confidence", BigQueryDbType.Float64, p.Confidence),
            new("title", BigQueryDbType.String, p.Title),
            new("forecast_summary", BigQueryDbType.String, p.ForecastSummary),
            new("recommended_preventive_action", BigQueryDbType.String, p.RecommendedPreventiveAction),
            new("supporting_facts_json", BigQueryDbType.String, supportingFactsJson),
            new("raw_context_json", BigQueryDbType.String, rawContext),
            new("model_output_json", BigQueryDbType.String, modelOutput),
            new("model_name", BigQueryDbType.String, _options.ModelName),
            new("prompt_version", BigQueryDbType.String, _options.PromptVersion),
            new("created_at", BigQueryDbType.Timestamp, now),
            new("updated_at", BigQueryDbType.Timestamp, now),
        };

        await _bq.ExecuteDmlAsync(mergeSql, parameters, cancellationToken).ConfigureAwait(false);
    }

    private sealed class ConcurrentCounter
    {
        private int _v;

        public int Value => _v;

        public void Increment() => Interlocked.Increment(ref _v);
    }
}
