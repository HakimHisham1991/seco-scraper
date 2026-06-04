using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using SecoItemHarvester.Web.Models;

namespace SecoItemHarvester.Web.Services;

public class CsvExportService : ICsvExportService
{
    public async Task<byte[]> ExportAsync(IEnumerable<ItemLookup> items, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await using var writer = new StreamWriter(memory, Encoding.UTF8);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteField("ItemNumber");
        csv.WriteField("ItemDescription");
        await csv.NextRecordAsync();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            csv.WriteField(item.ItemNumber);
            csv.WriteField(item.ItemDescription ?? string.Empty);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
        return memory.ToArray();
    }
}
