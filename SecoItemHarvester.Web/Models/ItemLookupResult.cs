namespace SecoItemHarvester.Web.Models;

public class ItemLookupResult
{
    public string ItemNumber { get; set; } = string.Empty;
    public string? ItemDescription { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Source { get; set; } = string.Empty;
}
