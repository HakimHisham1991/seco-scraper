namespace SecoItemHarvester.Web.Options;

public class DeepSeekOptions
{
    public const string SectionName = "DeepSeek";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "deepseek-chat";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
    public bool UseOpenAiCompatibleApi { get; set; }
}
