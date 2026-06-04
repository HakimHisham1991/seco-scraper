namespace SecoItemHarvester.Web.Services;

public static class ItemNumberNormalizer
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var value = raw.Trim();
        if (value.StartsWith("p_", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }

    public static string BuildProductUrl(string itemNumber) =>
        $"https://www.secotools.com/article/p_{itemNumber}";
}
