using SecoItemHarvester.Web.Models;

namespace SecoItemHarvester.Web.Services;

public interface ICsvExportService
{
    Task<byte[]> ExportAsync(IEnumerable<ItemLookup> items, CancellationToken cancellationToken = default);
}
