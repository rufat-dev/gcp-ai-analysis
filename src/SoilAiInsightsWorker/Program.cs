using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using SoilAiInsightsWorker;
using SoilAiInsightsWorker.Ai;
using SoilAiInsightsWorker.Endpoints;
using SoilAiInsightsWorker.Models;
using SoilAiInsightsWorker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
    o.UseUtcTimestamp = true;
});

builder.Services.Configure<AiInsightsWorkerOptions>(
    builder.Configuration.GetSection(AiInsightsWorkerOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiInsightsWorkerOptions>>().Value;
    var tz = Environment.GetEnvironmentVariable("AI_INSIGHTS_TIMEZONE");
    if (!string.IsNullOrWhiteSpace(tz))
        opt.InsightTimeZone = tz.Trim();
    var fcmEnv = Environment.GetEnvironmentVariable("AI_INSIGHTS_FCM_ENABLED");
    if (string.Equals(fcmEnv, "false", StringComparison.OrdinalIgnoreCase))
        opt.FcmNotificationsEnabled = false;
    if (string.Equals(Environment.GetEnvironmentVariable("AI_INSIGHTS_ALERT_PERSIST"), "false", StringComparison.OrdinalIgnoreCase))
        opt.PersistAlertsAfterFcmPush = false;
    return opt;
});

await TryInitFirebaseForFcmAsync();

builder.Services.AddSingleton<BigQueryTableResolver>();
builder.Services.AddSingleton<BigQueryCommandRunner>();
builder.Services.AddSingleton<SqlTemplateProvider>();
builder.Services.AddSingleton<PushAlertInserter>();
builder.Services.AddSingleton<FcmPushService>();
builder.Services.AddSingleton<IFcmPushService>(sp => sp.GetRequiredService<FcmPushService>());
builder.Services.AddSingleton<AiInsightsJobOrchestrator>();

builder.Services.AddHttpClient("openai", (sp, c) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["AiInsightsWorker:OpenAiApiBaseUrl"] ?? "https://api.openai.com/v1/";
    c.BaseAddress = new Uri(baseUrl);
    var timeoutSec = int.TryParse(cfg["AiInsightsWorker:OpenAiTimeoutSeconds"], out var t) ? t : 120;
    c.Timeout = TimeSpan.FromSeconds(timeoutSec);
});

builder.Services.AddSingleton<HeuristicAiInsightGenerator>();
builder.Services.AddSingleton<OpenAiAiInsightGenerator>();
builder.Services.AddSingleton<IAiInsightGenerator>(sp =>
{
    var opt = sp.GetRequiredService<AiInsightsWorkerOptions>();
    if (string.Equals(opt.AiProvider, "OpenAi", StringComparison.OrdinalIgnoreCase))
        return sp.GetRequiredService<OpenAiAiInsightGenerator>();
    return sp.GetRequiredService<HeuristicAiInsightGenerator>();
});

var app = builder.Build();

app.MapAiInsightsEndpoints();
app.Run();

static async Task TryInitFirebaseForFcmAsync()
{
    var fcmDisabled = string.Equals(
        Environment.GetEnvironmentVariable("AI_INSIGHTS_FCM_ENABLED"),
        "false",
        StringComparison.OrdinalIgnoreCase);
    Console.WriteLine(
        $"SoilAiInsightsWorker: FCM initialization check. disabled={fcmDisabled} firebaseAppExists={FirebaseApp.DefaultInstance is not null}");

    if (fcmDisabled)
        return;

    var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")?.Trim();
    if (string.IsNullOrEmpty(projectId))
    {
        Console.Error.WriteLine(
            "SoilAiInsightsWorker: FIREBASE_PROJECT_ID is empty; skipping Firebase initialization.");
        return;
    }

    Console.WriteLine(
        $"SoilAiInsightsWorker: Firebase target project id configured as {projectId}");

    try
    {
        await LogRuntimeServiceAccountAsync().ConfigureAwait(false);

        // Cross-project FCM:
        // Cloud Run runs in Project A using its runtime service account.
        // Firebase/FCM lives in Project B.
        // Project B must grant Project A's runtime service account roles/firebasecloudmessaging.admin.
        // FIREBASE_PROJECT_ID must be Project B's Firebase project ID.
        var scopedCredential = GoogleCredential.GetApplicationDefault()
            .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

        var tokenDebugEnabled = string.Equals(
            Environment.GetEnvironmentVariable("AI_INSIGHTS_FCM_DEBUG_TOKEN"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (tokenDebugEnabled)
        {
            var token = await scopedCredential
                .GetAccessTokenForRequestAsync()
                .ConfigureAwait(false);
            var tokenPrefix = string.IsNullOrWhiteSpace(token)
                ? "(empty)"
                : token[..Math.Min(12, token.Length)];
            Console.WriteLine(
                $"SoilAiInsightsWorker: FCM debug token acquired={!string.IsNullOrWhiteSpace(token)} prefix={tokenPrefix}...");
        }

        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = scopedCredential,
                ProjectId = projectId,
            });
            Console.WriteLine(
                $"SoilAiInsightsWorker: FirebaseApp initialized for project {projectId}");
        }
        else
        {
            Console.WriteLine(
                "SoilAiInsightsWorker: FirebaseApp already initialized; reusing existing instance.");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"SoilAiInsightsWorker: Firebase initialization failed. {ex}");
        var strictInit = string.Equals(
            Environment.GetEnvironmentVariable("AI_INSIGHTS_FCM_STRICT_INIT"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (strictInit)
            throw;
    }
}

static async Task LogRuntimeServiceAccountAsync()
{
    const string metadataUrl =
        "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/email";
    try
    {
        using var http = new HttpClient();
        using var req = new HttpRequestMessage(HttpMethod.Get, metadataUrl);
        req.Headers.Add("Metadata-Flavor", "Google");
        using var res = await http.SendAsync(req).ConfigureAwait(false);
        var body = (await res.Content.ReadAsStringAsync().ConfigureAwait(false)).Trim();
        if (res.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(body))
        {
            Console.WriteLine(
                $"SoilAiInsightsWorker: Cloud Run runtime service account: {body}");
        }
        else
        {
            Console.Error.WriteLine(
                $"SoilAiInsightsWorker: Unable to resolve runtime service account from metadata server. status={(int)res.StatusCode} body={body}");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"SoilAiInsightsWorker: Metadata server runtime SA lookup failed. {ex}");
    }
}
