using SecoItemHarvester.Web.Dtos;
using SecoItemHarvester.Web.Models;

namespace SecoItemHarvester.Web.Repositories;

public interface IItemLookupRepository
{
    Task<int> ClearAllAsync(CancellationToken cancellationToken = default);
    Task<int> AddPendingItemsAsync(IEnumerable<string> itemNumbers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemLookup>> ClaimPendingBatchAsync(int batchSize, CancellationToken cancellationToken = default);
    Task UpdateResultAsync(long id, ItemLookupResult result, CancellationToken cancellationToken = default);
    Task ResetFailedToPendingAsync(CancellationToken cancellationToken = default);
    Task<ProcessingStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemLookupDto>> GetResultsAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemLookup>> GetCompletedForExportAsync(CancellationToken cancellationToken = default);
}
