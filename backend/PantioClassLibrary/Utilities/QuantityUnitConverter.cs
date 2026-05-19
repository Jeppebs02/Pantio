using PantioClassLibrary.Enums;

namespace PantioClassLibrary.Utilities;

public static class QuantityUnitConverter
{
    private static readonly HashSet<QuantityUnit> VolumeUnits =
    [
        QuantityUnit.L,
        QuantityUnit.Dl,
        QuantityUnit.Cl,
        QuantityUnit.Ml
    ];

    private static readonly HashSet<QuantityUnit> WeightUnits =
    [
        QuantityUnit.Kg,
        QuantityUnit.G,
        QuantityUnit.Mg
    ];

    public static bool AreSameCategory(QuantityUnit a, QuantityUnit b) =>
        (VolumeUnits.Contains(a) && VolumeUnits.Contains(b)) ||
        (WeightUnits.Contains(a) && WeightUnits.Contains(b));

    public static decimal Convert(decimal quantity, QuantityUnit from, QuantityUnit to)
    {
        if (!AreSameCategory(from, to))
            throw new InvalidOperationException(
                $"Cannot convert between {from} and {to}: incompatible unit categories.");

        decimal baseValue = quantity * GetFactor(from);

        return baseValue / GetFactor(to);
    }

    private static decimal GetFactor(QuantityUnit unit) => unit switch
    {
        // Volume -> ml
        QuantityUnit.L  => 1_000m,
        QuantityUnit.Dl => 100m,
        QuantityUnit.Cl => 10m,
        QuantityUnit.Ml => 1m,

        // Weight -> mg
        QuantityUnit.Kg => 1_000_000m,
        QuantityUnit.G  => 1_000m,
        QuantityUnit.Mg => 1m,

        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };
}
