using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace SecoItemHarvester.Web.Services;

public class ItemInputParser : IItemInputParser
{
    public IReadOnlyList<string> ParseText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(['\r', '\n', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ItemNumberNormalizer.Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> ParseCsvAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        });

        var results = new List<string>();
        if (!await csv.ReadAsync())
        {
            return results;
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        var itemColumnIndex = FindItemColumnIndex(headers);

        if (itemColumnIndex >= 0)
        {
            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = csv.GetField(itemColumnIndex);
                var normalized = ItemNumberNormalizer.Normalize(value);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    results.Add(normalized);
                }
            }
        }
        else
        {
            stream.Position = 0;
            using var plainReader = new StreamReader(stream);
            while (await plainReader.ReadLineAsync(cancellationToken) is { } line)
            {
                var normalized = ItemNumberNormalizer.Normalize(line);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    results.Add(normalized);
                }
            }
        }

        return results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static int FindItemColumnIndex(string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim();
            if (header.Equals("ItemNumber", StringComparison.OrdinalIgnoreCase)
                || header.Equals("Item Number", StringComparison.OrdinalIgnoreCase)
                || header.Equals("Item", StringComparison.OrdinalIgnoreCase)
                || header.Equals("Catalog", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return headers.Length > 0 ? 0 : -1;
    }
}
