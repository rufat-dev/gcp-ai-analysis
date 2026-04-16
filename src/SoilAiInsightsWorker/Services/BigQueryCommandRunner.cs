using Google.Cloud.BigQuery.V2;

namespace SoilAiInsightsWorker.Services;

public sealed class BigQueryCommandRunner
{
    private readonly BigQueryTableResolver _resolver;
    private readonly ILogger<BigQueryCommandRunner> _logger;

    public BigQueryCommandRunner(BigQueryTableResolver resolver, ILogger<BigQueryCommandRunner> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    public string ProjectId => _resolver.ProjectId;

    public async Task<BigQueryResults> ExecuteQueryAsync(
        string sql,
        IEnumerable<BigQueryParameter>? parameters,
        CancellationToken cancellationToken = default)
    {
        var client = await BigQueryClient.CreateAsync(ProjectId).ConfigureAwait(false);
        var paramList = parameters?.ToList() ?? [];
        var options = new QueryOptions { UseLegacySql = false };

        var job = await client
            .CreateQueryJobAsync(sql, paramList, options, cancellationToken)
            .ConfigureAwait(false);

        await job.PollUntilCompletedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (job.Status.ErrorResult != null)
        {
            _logger.LogError("BigQuery job failed: {Message}", job.Status.ErrorResult.Message);
            throw new InvalidOperationException($"BigQuery query failed: {job.Status.ErrorResult.Message}");
        }

        return await client.GetQueryResultsAsync(job.Reference.JobId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Runs DML (MERGE) and returns affected row count when available.</summary>
    public async Task<long> ExecuteDmlAsync(
        string sql,
        IEnumerable<BigQueryParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        var client = await BigQueryClient.CreateAsync(ProjectId).ConfigureAwait(false);
        var options = new QueryOptions { UseLegacySql = false };

        var job = await client
            .CreateQueryJobAsync(sql, parameters.ToList(), options, cancellationToken)
            .ConfigureAwait(false);

        await job.PollUntilCompletedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (job.Status.ErrorResult != null)
        {
            _logger.LogError("BigQuery DML failed: {Message}", job.Status.ErrorResult.Message);
            throw new InvalidOperationException($"BigQuery DML failed: {job.Status.ErrorResult.Message}");
        }

        var stats = job.Statistics?.Query?.DmlStats;
        return stats?.InsertedRowCount + stats?.UpdatedRowCount ?? 0;
    }
}
