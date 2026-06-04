using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using SecoItemHarvester.Web.Models;
using SecoItemHarvester.Web.Options;

namespace SecoItemHarvester.Web.Services;

public class DeepSeekClient : IDeepSeekClient
{
    private const string SourceName = "DeepSeek";

    private readonly HttpClient _httpClient;
    private readonly DeepSeekOptions _options;
    private readonly ILogger<DeepSeekClient> _logger;
    private readonly TokenBucketRateLimiter _rateLimiter;

    public DeepSeekClient(HttpClient httpClient, IOptions<DeepSeekOptions> options, ILogger<DeepSeekClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = 5,
            AutoReplenishment = true
        });
    }

    public async Task<ItemLookupResult> LookupItemAsync(string itemNumber, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Fail(itemNumber, "DeepSeek is disabled in configuration.");
        }

        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);
        if (!lease.IsAcquired)
        {
            return Fail(itemNumber, "DeepSeek rate limit exceeded.");
        }

        var url = ItemNumberNormalizer.BuildProductUrl(itemNumber);
        var prompt =
            "You are a web research agent.\n\n" +
            $"Open this URL:\n\n{url}\n\n" +
            "Extract the product Item Description.\n\n" +
            "Return ONLY valid JSON.\n\n" +
            "Schema:\n\n" +
            $"{{\"itemNumber\": \"{itemNumber}\", \"itemDescription\": \"...\"}}";

        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogInformation(
                    "DeepSeek lookup started for item {ItemNumber} (attempt {Attempt})",
                    itemNumber,
                    attempt);

                var responseText = await SendPromptAsync(prompt, cancellationToken);
                var parsed = DeepSeekResponseParser.TryParse(responseText, itemNumber);

                if (!DeepSeekResponseParser.IsValid(parsed, itemNumber))
                {
                    _logger.LogWarning(
                        "DeepSeek returned invalid JSON for item {ItemNumber} on attempt {Attempt}",
                        itemNumber,
                        attempt);

                    if (attempt >= _options.MaxRetries)
                    {
                        return Fail(itemNumber, "DeepSeek returned malformed JSON or missing itemDescription.");
                    }

                    await Task.Delay(_options.RetryDelayMilliseconds * attempt, cancellationToken);
                    continue;
                }

                sw.Stop();
                _logger.LogInformation(
                    "DeepSeek lookup succeeded for item {ItemNumber} in {ElapsedMs}ms",
                    itemNumber,
                    sw.ElapsedMilliseconds);

                return new ItemLookupResult
                {
                    ItemNumber = itemNumber,
                    ItemDescription = parsed!.ItemDescription!.Trim(),
                    Success = true,
                    Source = SourceName
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "DeepSeek attempt {Attempt} failed for item {ItemNumber}",
                    attempt,
                    itemNumber);

                if (attempt >= _options.MaxRetries)
                {
                    sw.Stop();
                    _logger.LogError(
                        ex,
                        "DeepSeek lookup failed for item {ItemNumber} after {Attempts} attempts ({ElapsedMs}ms)",
                        itemNumber,
                        attempt,
                        sw.ElapsedMilliseconds);

                    return Fail(itemNumber, ex.Message);
                }

                await Task.Delay(_options.RetryDelayMilliseconds * attempt, cancellationToken);
            }
        }

        return Fail(itemNumber, "DeepSeek request failed unexpectedly.");
    }

    private async Task<string> SendPromptAsync(string prompt, CancellationToken cancellationToken)
    {
        var useOpenAi = _options.UseOpenAiCompatibleApi || !string.IsNullOrWhiteSpace(_options.ApiKey);
        return useOpenAi
            ? await SendOpenAiChatAsync(prompt, cancellationToken)
            : await SendOllamaChatAsync(prompt, cancellationToken);
    }

    private async Task<string> SendOllamaChatAsync(string prompt, CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest
        {
            Model = _options.Model,
            Messages = [new OllamaChatMessage { Role = "user", Content = prompt }],
            Stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"DeepSeek returned HTTP {(int)response.StatusCode}: {body}");
        }

        var chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(body);
        if (string.IsNullOrWhiteSpace(chatResponse?.Message?.Content))
        {
            throw new InvalidOperationException("DeepSeek returned an empty response.");
        }

        return chatResponse.Message.Content.Trim();
    }

    private async Task<string> SendOpenAiChatAsync(string prompt, CancellationToken cancellationToken)
    {
        var request = new OpenAiChatRequest
        {
            Model = _options.Model,
            Messages = [new OpenAiChatMessage { Role = "user", Content = prompt }]
        };

        using var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"DeepSeek returned HTTP {(int)response.StatusCode}: {body}");
        }

        var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(body);
        var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("DeepSeek returned an empty response.");
        }

        return content.Trim();
    }

    private static ItemLookupResult Fail(string itemNumber, string error) =>
        new()
        {
            ItemNumber = itemNumber,
            Success = false,
            Error = error,
            Source = SourceName
        };

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }
    }

    private sealed class OpenAiChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiChatMessage> Messages { get; set; } = [];
    }

    private sealed class OpenAiChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiChatMessage? Message { get; set; }
    }
}
