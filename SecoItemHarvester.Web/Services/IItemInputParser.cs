namespace SecoItemHarvester.Web.Services;

public interface IItemInputParser
{
    IReadOnlyList<string> ParseText(string? text);
    Task<IReadOnlyList<string>> ParseCsvAsync(Stream stream, CancellationToken cancellationToken = default);
}
