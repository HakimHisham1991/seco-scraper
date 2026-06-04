using Microsoft.Extensions.Options;
using SecoItemHarvester.Web.Options;
using SecoItemHarvester.Web.Repositories;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Web.Workers;

public class ItemProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProcessingTrigger _processingTrigger;
    private readonly ProcessingOptions _options;
    private readonly ILogger<ItemProcessingWorker> _logger;

    public ItemProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IProcessingTrigger processingTrigger,
        IOptions<ProcessingOptions> options,
        ILogger<ItemProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _processingTrigger = processingTrigger;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(_options.WorkerPollIntervalMilliseconds);

                try
                {
                    await _processingTrigger.WaitForSignalAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // poll interval elapsed
                }

                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in item processing worker loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Item processing worker stopped");
    }

    private async Task ProcessPendingAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IItemLookupRepository>();
            var batch = await repository.ClaimPendingBatchAsync(_options.BatchSize, stoppingToken);
            if (batch.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Processing batch of {Count} items", batch.Count);

            using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
            var tasks = batch.Select(item => ProcessOneAsync(item.Id, item.ItemNumber, semaphore, stoppingToken));
            await Task.WhenAll(tasks);
        }
    }

    private async Task ProcessOneAsync(
        long id,
        string itemNumber,
        SemaphoreSlim semaphore,
        CancellationToken stoppingToken)
    {
        await semaphore.WaitAsync(stoppingToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IItemLookupRepository>();
            var lookupService = scope.ServiceProvider.GetRequiredService<IItemLookupService>();

            var result = await lookupService.LookupAsync(itemNumber, stoppingToken);
            await repository.UpdateResultAsync(id, result, stoppingToken);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
