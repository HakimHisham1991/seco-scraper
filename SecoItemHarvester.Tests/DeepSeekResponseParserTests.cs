using FluentAssertions;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Tests;

public class DeepSeekResponseParserTests
{
    [Fact]
    public void TryParse_ParsesJsonBlock()
    {
        const string response = """
            Here is the result:
            ```json
            {"itemNumber":"02679365","itemDescription":"553055Z3.0-SIRON-A"}
            ```
            """;

        var parsed = DeepSeekResponseParser.TryParse(response, "02679365");

        parsed.Should().NotBeNull();
        DeepSeekResponseParser.IsValid(parsed, "02679365").Should().BeTrue();
        parsed!.ItemDescription.Should().Be("553055Z3.0-SIRON-A");
    }

    [Fact]
    public void IsValid_FailsWhenItemDescriptionMissing()
    {
        var parsed = DeepSeekResponseParser.TryParse("""{"itemNumber":"02679365"}""", "02679365");

        DeepSeekResponseParser.IsValid(parsed, "02679365").Should().BeFalse();
    }
}
