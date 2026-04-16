using System.Security.Cryptography;
using System.Text;
using SoilAiInsightsWorker.Models;
using SoilAiInsightsWorker.Services;

namespace SoilAiInsightsWorker.Endpoints;

public static class AiInsightsWorkerEndpoints
{
    public static void MapAiInsightsEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Text("OK"));

        app.MapPost("/internal/jobs/ai-insights/run", async (
            HttpContext http,
            AiInsightsJobOrchestrator orchestrator,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken,
            [Microsoft.AspNetCore.Mvc.FromQuery] string? mode,
            [Microsoft.AspNetCore.Mvc.FromQuery] string? deviceId,
            [Microsoft.AspNetCore.Mvc.FromBody] AiInsightsRunRequest? body) =>
        {
            var logger = loggerFactory.CreateLogger("AiInsightsWorkerEndpoints");
            var expectedKey = Environment.GetEnvironmentVariable("AI_INSIGHTS_WORKER_API_KEY");
            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                logger.LogError("AI_INSIGHTS_WORKER_API_KEY is not configured.");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (!http.Request.Headers.TryGetValue("X-Api-Key", out var provided) ||
                !FixedTimeEquals(provided.ToString(), expectedKey))
            {
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            var request = body ?? new AiInsightsRunRequest();
            if (!string.IsNullOrWhiteSpace(mode) && Enum.TryParse<AiInsightsRunMode>(mode, true, out var m))
                request.Mode = m;

            try
            {
                var result = await orchestrator.RunAsync(request, deviceId, cancellationToken).ConfigureAwait(false);
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI insights job failed.");
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        try
        {
            var ha = SHA256.HashData(Encoding.UTF8.GetBytes(a));
            var hb = SHA256.HashData(Encoding.UTF8.GetBytes(b));
            return CryptographicOperations.FixedTimeEquals(ha, hb);
        }
        catch
        {
            return false;
        }
    }
}
