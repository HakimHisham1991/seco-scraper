namespace SecoItemHarvester.Web.Services;

public interface ISecoProductScraper
{
    Task<ScrapePageResult> FetchPageAsync(string itemNumber, CancellationToken cancellationToken = default);
}

public class ScrapePageResult
{
    public string ItemNumber { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Html { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
}
