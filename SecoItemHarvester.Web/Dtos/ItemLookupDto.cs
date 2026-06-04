namespace SecoItemHarvester.Web.Dtos;

public class ItemLookupDto
{
    public long Id { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string? ItemDescription { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? Source { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
