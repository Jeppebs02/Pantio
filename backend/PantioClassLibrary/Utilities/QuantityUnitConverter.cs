using PantioClassLibrary.Enums;

namespace PantioClassLibrary.Utilities;

public static class QuantityUnitConverter
{
    private static readonly HashSet<QuantityUnit> VolumeUnits =
    [
        QuantityUnit.l,
        QuantityUnit.dl,
        QuantityUnit.cl,
        QuantityUnit.ml
    ];

    private static readonly HashSet<QuantityUnit> WeightUnits =
    [
        QuantityUnit.kg,
        QuantityUnit.g,
        QuantityUnit.mg
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
        QuantityUnit.l  => 1_000m,
        QuantityUnit.dl => 100m,
        QuantityUnit.cl => 10m,
        QuantityUnit.ml => 1m,

        // Weight -> mg
        QuantityUnit.kg => 1_000_000m,
        QuantityUnit.g  => 1_000m,
        QuantityUnit.mg => 1m,

        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };
}
