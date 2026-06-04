using System.Net;
using Microsoft.Extensions.Options;
using SecoItemHarvester.Web.Options;

namespace SecoItemHarvester.Web.Services;

public class SecoProductScraper : ISecoProductScraper
{
    private readonly HttpClient _httpClient;
    private readonly ProcessingOptions _options;
    private readonly ILogger<SecoProductScraper> _logger;

    public SecoProductScraper(
        HttpClient httpClient,
        IOptions<ProcessingOptions> processingOptions,
        ILogger<SecoProductScraper> logger)
    {
        _httpClient = httpClient;
        _options = processingOptions.Value;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
    }

    public async Task<ScrapePageResult> FetchPageAsync(string itemNumber, CancellationToken cancellationToken = default)
    {
        var url = ItemNumberNormalizer.BuildProductUrl(itemNumber);
        _logger.LogInformation("Seco scrape started for item {ItemNumber} at {Url}", itemNumber, url);

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ScrapePageResult
                {
                    ItemNumber = itemNumber,
                    Url = url,
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }

            if (string.IsNullOrWhiteSpace(html) || !html.Contains(itemNumber, StringComparison.Ordinal))
            {
                return new ScrapePageResult
                {
                    ItemNumber = itemNumber,
                    Url = url,
                    Html = html,
                    Success = false,
                    StatusCode = (int)response.StatusCode,
                    Error = "Page did not contain expected item content."
                };
            }

            _logger.LogInformation("Seco scrape finished for item {ItemNumber}", itemNumber);

            return new ScrapePageResult
            {
                ItemNumber = itemNumber,
                Url = url,
                Html = html,
                Success = true,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Seco scrape timed out for item {ItemNumber}", itemNumber);
            return new ScrapePageResult
            {
                ItemNumber = itemNumber,
                Url = url,
                Success = false,
                Error = "Network timeout while downloading Seco page."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seco scrape failed for item {ItemNumber}", itemNumber);
            return new ScrapePageResult
            {
                ItemNumber = itemNumber,
                Url = url,
                Success = false,
                Error = ex.Message
            };
        }
    }
}
