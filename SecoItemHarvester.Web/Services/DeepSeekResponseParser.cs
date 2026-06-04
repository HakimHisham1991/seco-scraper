using System.Text.Json;
using System.Text.RegularExpressions;
using SecoItemHarvester.Web.Models;

namespace SecoItemHarvester.Web.Services;

public static class DeepSeekResponseParser
{
    private static readonly Regex JsonBlockPattern = new(
        @"```(?:json)?\s*(?<json>\{.*?\})\s*```",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public static DeepSeekItemResponse? TryParse(string response, string expectedItemNumber)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var json = response.Trim();
        var blockMatch = JsonBlockPattern.Match(json);
        if (blockMatch.Success)
        {
            json = blockMatch.Groups["json"].Value;
        }
        else
        {
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                json = json[start..(end + 1)];
            }
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<DeepSeekItemResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is not null && string.IsNullOrWhiteSpace(parsed.ItemNumber))
            {
                parsed.ItemNumber = expectedItemNumber;
            }

            return parsed;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool IsValid(DeepSeekItemResponse? parsed, string expectedItemNumber)
    {
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.ItemDescription))
        {
            return false;
        }

        var normalizedExpected = ItemNumberNormalizer.Normalize(expectedItemNumber);
        var normalizedActual = ItemNumberNormalizer.Normalize(parsed.ItemNumber);
        return string.Equals(normalizedExpected, normalizedActual, StringComparison.OrdinalIgnoreCase);
    }
}
