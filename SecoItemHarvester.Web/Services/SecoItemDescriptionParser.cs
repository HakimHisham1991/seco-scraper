using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace SecoItemHarvester.Web.Services;

public static class SecoItemDescriptionParser
{
    private static readonly Regex TitlePattern = new(
        @"^(?<desc>.+?)\s*\|\s*Seco Tools\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DesignationPattern = new(
        @"<b>Designation</b>\s*:\s*(?<desc>[^<]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MetaDescriptionPattern = new(
        @"meta\s+name=""description""\s+content=""(?<desc>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string? Parse(string? html, string itemNumber)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode is not null)
        {
            var match = TitlePattern.Match(titleNode.InnerText.Trim());
            if (match.Success)
            {
                return match.Groups["desc"].Value.Trim();
            }
        }

        var h1 = doc.DocumentNode.SelectSingleNode("//h1");
        if (h1 is not null && !string.IsNullOrWhiteSpace(h1.InnerText))
        {
            return HtmlEntity.DeEntitize(h1.InnerText.Trim());
        }

        var designationMatch = DesignationPattern.Match(html);
        if (designationMatch.Success)
        {
            return designationMatch.Groups["desc"].Value.Trim();
        }

        var metaMatch = MetaDescriptionPattern.Match(html);
        if (metaMatch.Success)
        {
            var content = metaMatch.Groups["desc"].Value.Trim();
            var normalizedItem = ItemNumberNormalizer.Normalize(itemNumber);
            if (content.StartsWith(normalizedItem, StringComparison.Ordinal))
            {
                return null;
            }

            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0].Contains('.', StringComparison.Ordinal))
            {
                return parts[0];
            }
        }

        return TryParseEmbeddedJson(html, itemNumber);
    }

    private static string? TryParseEmbeddedJson(string html, string itemNumber)
    {
        foreach (var jsonFragment in ExtractJsonObjects(html))
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonFragment);
                var description = FindDescriptionProperty(doc.RootElement, itemNumber);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return description;
                }
            }
            catch (JsonException)
            {
                // continue
            }
        }

        return null;
    }

    private static IEnumerable<string> ExtractJsonObjects(string html)
    {
        for (var i = 0; i < html.Length; i++)
        {
            if (html[i] != '{')
            {
                continue;
            }

            var depth = 0;
            for (var j = i; j < html.Length; j++)
            {
                if (html[j] == '{')
                {
                    depth++;
                }
                else if (html[j] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        var length = j - i + 1;
                        if (length is > 20 and < 500_000
                            && html.AsSpan(i, Math.Min(length, 2000)).Contains("description", StringComparison.OrdinalIgnoreCase))
                        {
                            yield return html[i..(j + 1)];
                        }

                        break;
                    }
                }
            }
        }
    }

    private static string? FindDescriptionProperty(JsonElement element, string itemNumber)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Contains("description", StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(value)
                            && !value.Contains(itemNumber, StringComparison.Ordinal))
                        {
                            return value;
                        }
                    }

                    var nested = FindDescriptionProperty(property.Value, itemNumber);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    var nested = FindDescriptionProperty(child, itemNumber);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                break;
        }

        return null;
    }
}
