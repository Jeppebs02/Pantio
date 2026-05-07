using PantioClassLibrary.Entities;

namespace PantioAPI.Services;

internal static class RecipeIngredientMatcher
{
    public static InventoryItem? FindBestMatch(string name, List<InventoryItem> items)
    {
        var needle = Normalize(name);
        return items.FirstOrDefault(i => Normalize(i.ProductName) == needle)
            ?? items.FirstOrDefault(i =>
                Normalize(i.ProductName).Contains(needle) ||
                needle.Contains(Normalize(i.ProductName)));
    }

    private static string Normalize(string s) =>
        s.Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");
}
