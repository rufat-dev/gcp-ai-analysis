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

TryInitFirebaseForFcm();

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

static void TryInitFirebaseForFcm()
{
    if (string.Equals(Environment.GetEnvironmentVariable("AI_INSIGHTS_FCM_ENABLED"), "false", StringComparison.OrdinalIgnoreCase))
        return;

    var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")?.Trim();
    if (string.IsNullOrEmpty(projectId))
        return;

    try
    {
        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault(),
                ProjectId = projectId,
            });
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"SoilAiInsightsWorker: Firebase init skipped; FCM will no-op ({ex.Message})");
    }
}
