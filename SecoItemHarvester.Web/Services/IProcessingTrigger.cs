namespace SecoItemHarvester.Web.Services;

public interface IProcessingTrigger
{
    void RequestProcessing();
    Task WaitForSignalAsync(CancellationToken cancellationToken);
}
