using SecoItemHarvester.Web.Models;

namespace SecoItemHarvester.Web.Services;

public interface IItemLookupService
{
    Task<ItemLookupResult> LookupAsync(string itemNumber, CancellationToken cancellationToken = default);
}
