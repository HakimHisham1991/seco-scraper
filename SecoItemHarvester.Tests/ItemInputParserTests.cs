using FluentAssertions;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Tests;

public class ItemInputParserTests
{
    private readonly ItemInputParser _parser = new();

    [Fact]
    public void ParseText_NormalizesAndDeduplicates()
    {
        var result = _parser.ParseText("02679365\np_02679366\n02679365");

        result.Should().BeEquivalentTo(["02679365", "02679366"]);
    }

    [Fact]
    public async Task ParseCsvAsync_ReadsItemNumberColumn()
    {
        const string csv = """
            ItemNumber,ItemDescription
            02679365,
            02679366,
            """;

        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var result = await _parser.ParseCsvAsync(stream);

        result.Should().BeEquivalentTo(["02679365", "02679366"]);
    }
}
