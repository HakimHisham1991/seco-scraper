using System.Text;
using FluentAssertions;
using SecoItemHarvester.Web.Models;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Tests;

public class CsvExportServiceTests
{
    [Fact]
    public async Task ExportAsync_WritesHeaderAndRows()
    {
        var service = new CsvExportService();
        var items = new[]
        {
            new ItemLookup { ItemNumber = "02679365", ItemDescription = "553055Z3.0-SIRON-A" }
        };

        var bytes = await service.ExportAsync(items);
        var csv = Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("ItemNumber,ItemDescription");
        csv.Should().Contain("02679365,553055Z3.0-SIRON-A");
    }
}
