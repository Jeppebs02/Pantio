using System.Globalization;
using System.Text;
using PantioClassLibrary.Entities;

namespace PantioAPI.Services;

internal static class RecipeIngredientMatcher
{
    public static InventoryItem? FindBestMatch(string name, List<InventoryItem> items)
    {
        var needle = Normalize(name);
        var needleWords = Words(needle);

        return items.FirstOrDefault(i => Normalize(i.ProductName) == needle)
            ?? items.FirstOrDefault(i =>
            {
                var hayWords = Words(Normalize(i.ProductName));
                if (needleWords.IsSubsetOf(hayWords) && hayWords.Count <= needleWords.Count + 1)
                    return true;
                return hayWords.IsSubsetOf(needleWords);
            });
    }

    private static HashSet<string> Words(string s) =>
        s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

    private static string Normalize(string s)
    {
        var lower = s.Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");
        var decomposed = lower.Normalize(NormalizationForm.FormD);
        var stripped = new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return stripped.Normalize(NormalizationForm.FormC);
    }
}