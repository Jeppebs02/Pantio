using System.Globalization;
using System.Text.RegularExpressions;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Enums;

namespace PantioClassLibrary.Utilities;

public static partial class OffQuantityParser
{
    // Matches "6 x 330 ml" (multi-pack) — captures per-item amount and unit
    [GeneratedRegex(@"\d+\s*[xX×]\s*(?<qty>[\d.,]+)\s*(?<unit>[a-zA-Z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex MultiPackPattern();

    // Matches "500 ml", "1.5 kg", "1,5 L" — simple amount + unit
    [GeneratedRegex(@"(?<qty>[\d.,]+)\s*(?<unit>[a-zA-Z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex SimplePattern();

    private static readonly Dictionary<string, QuantityUnit> UnitMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["l"]  = QuantityUnit.L,
        ["dl"] = QuantityUnit.Dl,
        ["cl"] = QuantityUnit.Cl,
        ["ml"] = QuantityUnit.Ml,
        ["kg"] = QuantityUnit.Kg,
        ["g"]  = QuantityUnit.G,
        ["mg"] = QuantityUnit.Mg,
    };

    public static OffProductData NormalizeData(OffProductData data)
    {
        if (data.Quantity is null || data.QuantityUnit is null || data.Quantity.Value >= 1m)
            return data;
        var (qty, unit) = Normalize(data.Quantity.Value, data.QuantityUnit.Value);
        return data with { Quantity = qty, QuantityUnit = unit };
    }

    public static (decimal? Quantity, QuantityUnit? Unit) Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, null);

        var multiMatch = MultiPackPattern().Match(raw);
        if (multiMatch.Success)
            return ExtractFromMatch(multiMatch);

        var simpleMatch = SimplePattern().Match(raw);
        if (simpleMatch.Success)
            return ExtractFromMatch(simpleMatch);

        return (null, null);
    }

    private static readonly QuantityUnit[] VolumeLadder = [QuantityUnit.L, QuantityUnit.Dl, QuantityUnit.Cl, QuantityUnit.Ml];
    private static readonly QuantityUnit[] WeightLadder = [QuantityUnit.Kg, QuantityUnit.G, QuantityUnit.Mg];

    private static (decimal? Quantity, QuantityUnit? Unit) ExtractFromMatch(Match m)
    {
        var qtyStr = m.Groups["qty"].Value.Replace(',', '.');
        var unitStr = m.Groups["unit"].Value;

        if (!decimal.TryParse(qtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
            return (null, null);

        if (!UnitMap.TryGetValue(unitStr, out var unit))
            return (null, null);

        return Normalize(qty, unit);
    }

    private static (decimal Quantity, QuantityUnit Unit) Normalize(decimal qty, QuantityUnit unit)
    {
        if (qty >= 1m) return (qty, unit);

        var ladder = VolumeLadder.Contains(unit) ? VolumeLadder : WeightLadder;
        var startIndex = Array.IndexOf(ladder, unit);

        for (var i = startIndex + 1; i < ladder.Length; i++)
        {
            var converted = QuantityUnitConverter.Convert(qty, unit, ladder[i]);
            if (converted >= 1m)
                return (converted, ladder[i]);
        }

        return (qty, unit);
    }
}
