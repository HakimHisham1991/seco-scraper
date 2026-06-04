using Microsoft.Extensions.Options;
using SecoItemHarvester.Web.Models;
using SecoItemHarvester.Web.Options;

namespace SecoItemHarvester.Web.Services;

public class ItemLookupService : IItemLookupService
{
    private const string ScraperSource = "Scraper";

    private readonly IDeepSeekClient _deepSeekClient;
    private readonly ISecoProductScraper _scraper;
    private readonly DeepSeekOptions _deepSeekOptions;
    private readonly ILogger<ItemLookupService> _logger;

    public ItemLookupService(
        IDeepSeekClient deepSeekClient,
        ISecoProductScraper scraper,
        IOptions<DeepSeekOptions> deepSeekOptions,
        ILogger<ItemLookupService> logger)
    {
        _deepSeekClient = deepSeekClient;
        _scraper = scraper;
        _deepSeekOptions = deepSeekOptions.Value;
        _logger = logger;
    }

    public async Task<ItemLookupResult> LookupAsync(string itemNumber, CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Lookup started for item {ItemNumber}", itemNumber);

        if (_deepSeekOptions.Enabled)
        {
            try
            {
                var deepSeekResult = await _deepSeekClient.LookupItemAsync(itemNumber, cancellationToken);
                if (deepSeekResult.Success)
                {
                    sw.Stop();
                    _logger.LogInformation(
                        "Lookup succeeded via DeepSeek for {ItemNumber} in {ElapsedMs}ms",
                        itemNumber,
                        sw.ElapsedMilliseconds);
                    return deepSeekResult;
                }

                _logger.LogWarning(
                    "DeepSeek failed for {ItemNumber}: {Error}. Trying scraper fallback.",
                    itemNumber,
                    deepSeekResult.Error);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeepSeek unavailable for {ItemNumber}. Trying scraper fallback.", itemNumber);
            }
        }

        var fallback = await LookupWithScraperAsync(itemNumber, cancellationToken);
        sw.Stop();
        _logger.LogInformation(
            "Lookup finished for {ItemNumber} via {Source} success={Success} in {ElapsedMs}ms",
            itemNumber,
            fallback.Source,
            fallback.Success,
            sw.ElapsedMilliseconds);

        return fallback;
    }

    private async Task<ItemLookupResult> LookupWithScraperAsync(string itemNumber, CancellationToken cancellationToken)
    {
        var page = await _scraper.FetchPageAsync(itemNumber, cancellationToken);
        if (!page.Success || string.IsNullOrWhiteSpace(page.Html))
        {
            return new ItemLookupResult
            {
                ItemNumber = itemNumber,
                Success = false,
                Source = ScraperSource,
                Error = page.Error ?? "Unable to download Seco product page."
            };
        }

        var description = SecoItemDescriptionParser.Parse(page.Html, itemNumber);
        if (string.IsNullOrWhiteSpace(description))
        {
            return new ItemLookupResult
            {
                ItemNumber = itemNumber,
                Success = false,
                Source = ScraperSource,
                Error = "Item Description was not found in the downloaded page."
            };
        }

        return new ItemLookupResult
        {
            ItemNumber = itemNumber,
            ItemDescription = description,
            Success = true,
            Source = ScraperSource
        };
    }
}
