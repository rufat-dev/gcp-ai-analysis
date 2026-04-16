using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoilAiInsightsWorker.Models;

namespace SoilAiInsightsWorker.Ai;

public sealed class OpenAiAiInsightGenerator : IAiInsightGenerator
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAiAiInsightGenerator> _logger;
    private readonly string _model;

    public OpenAiAiInsightGenerator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAiAiInsightGenerator> logger)
    {
        _http = httpClientFactory.CreateClient("openai");
        _model = configuration["AiInsightsWorker:OpenAiChatModel"] ?? "gpt-4o-mini";
        _logger = logger;
    }

    public async Task<RecommendationAiPayload> GenerateRecommendationAsync(
        string systemPrompt,
        string userJsonPayload,
        CancellationToken cancellationToken)
    {
        var json = await ChatJsonAsync(systemPrompt, userJsonPayload, cancellationToken).ConfigureAwait(false);
        var parsed = AiResponseValidator.TryParseRecommendation(json);
        if (parsed is null)
            throw new InvalidOperationException("OpenAI returned JSON that could not be parsed as RecommendationAiPayload.");
        return parsed;
    }

    public async Task<ForecastAiPayload> GenerateForecastAsync(
        string systemPrompt,
        string userJsonPayload,
        int horizonHours,
        CancellationToken cancellationToken)
    {
        var json = await ChatJsonAsync(systemPrompt, userJsonPayload, cancellationToken).ConfigureAwait(false);
        var parsed = AiResponseValidator.TryParseForecast(json);
        if (parsed is null)
            throw new InvalidOperationException("OpenAI returned JSON that could not be parsed as ForecastAiPayload.");
        parsed.ForecastHorizonHours = horizonHours;
        return parsed;
    }

    private async Task<string> ChatJsonAsync(string systemPrompt, string userJsonPayload, CancellationToken cancellationToken)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not set.");

        var body = new ChatRequest
        {
            Model = _model,
            ResponseFormat = new ResponseFormatObj { Type = "json_object" },
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = "Context JSON:\n" + userJsonPayload },
            ],
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI HTTP {Status}: {Body}", (int)resp.StatusCode, text);
            throw new InvalidOperationException($"OpenAI request failed: {(int)resp.StatusCode}");
        }

        using var doc = JsonDocument.Parse(text);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI returned empty content.");
        return content;
    }

    private sealed class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("response_format")]
        public ResponseFormatObj? ResponseFormat { get; set; }
    }

    private sealed class ResponseFormatObj
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "json_object";
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
