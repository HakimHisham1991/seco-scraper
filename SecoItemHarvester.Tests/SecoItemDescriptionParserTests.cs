using FluentAssertions;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Tests;

public class SecoItemDescriptionParserTests
{
    [Fact]
    public void Parse_ExtractsDescription_FromTitle()
    {
        const string html = """
            <html><head><title>553055Z3.0-SIRON-A | Seco Tools</title></head><body></body></html>
            """;

        var result = SecoItemDescriptionParser.Parse(html, "02679365");

        result.Should().Be("553055Z3.0-SIRON-A");
    }

    [Fact]
    public void Parse_ExtractsDescription_FromDesignationField()
    {
        const string html = """
            <html><body><ul><li><b>Designation</b>: ABC-123-TOOL</li></ul></body></html>
            """;

        var result = SecoItemDescriptionParser.Parse(html, "02679365");

        result.Should().Be("ABC-123-TOOL");
    }

    [Fact]
    public void Parse_ReturnsNull_WhenHtmlMissingDescription()
    {
        const string html = "<html><head><title>Seco Tools</title></head><body></body></html>";

        var result = SecoItemDescriptionParser.Parse(html, "02679365");

        result.Should().BeNull();
    }
}
