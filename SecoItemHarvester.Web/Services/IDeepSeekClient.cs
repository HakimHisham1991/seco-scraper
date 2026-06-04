using SecoItemHarvester.Web.Models;

namespace SecoItemHarvester.Web.Services;

public interface IDeepSeekClient
{
    Task<ItemLookupResult> LookupItemAsync(string itemNumber, CancellationToken cancellationToken = default);
}
