namespace SecoItemHarvester.Web.Models;

public class ItemLookup
{
    public long Id { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string? ItemDescription { get; set; }
    public ItemLookupStatus Status { get; set; } = ItemLookupStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
