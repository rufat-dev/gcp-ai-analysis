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
    return opt;
});

builder.Services.AddSingleton<BigQueryTableResolver>();
builder.Services.AddSingleton<BigQueryCommandRunner>();
builder.Services.AddSingleton<SqlTemplateProvider>();
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
