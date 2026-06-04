namespace SecoItemHarvester.Web.Services;

public class ProcessingTrigger : IProcessingTrigger
{
    private readonly SemaphoreSlim _signal = new(0, int.MaxValue);

    public void RequestProcessing() => _signal.Release();

    public Task WaitForSignalAsync(CancellationToken cancellationToken) =>
        _signal.WaitAsync(cancellationToken);
}
