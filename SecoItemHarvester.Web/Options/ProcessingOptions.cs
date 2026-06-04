namespace SecoItemHarvester.Web.Options;

public class ProcessingOptions
{
    public const string SectionName = "Processing";

    public int BatchSize { get; set; } = 20;
    public int MaxConcurrency { get; set; } = 5;
    public int RetryCount { get; set; } = 3;
    public int HttpTimeoutSeconds { get; set; } = 45;
    public int WorkerPollIntervalMilliseconds { get; set; } = 2000;
    public int RateLimitPerSecond { get; set; } = 10;
}
