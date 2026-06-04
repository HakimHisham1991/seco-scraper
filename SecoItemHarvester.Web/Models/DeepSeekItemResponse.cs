using System.Text.Json.Serialization;

namespace SecoItemHarvester.Web.Models;

public class DeepSeekItemResponse
{
    [JsonPropertyName("itemNumber")]
    public string? ItemNumber { get; set; }

    [JsonPropertyName("itemDescription")]
    public string? ItemDescription { get; set; }
}
