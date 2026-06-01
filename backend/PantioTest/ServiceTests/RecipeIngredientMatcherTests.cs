using PantioAPI.Services;
using PantioClassLibrary.Entities;

namespace PantioTest.ServiceTests;

public class RecipeIngredientMatcherTests
{
    private static InventoryItem Item(string name) => new() { Id = Guid.NewGuid(), ProductName = name };

    // Exact matches
    [Test]
    public void ExactMatch_ReturnsThatItem()
    {
        var items = new List<InventoryItem> { Item("Pasta"), Item("Løg") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("Pasta", items)?.ProductName, Is.EqualTo("Pasta"));
    }

    [Test]
    public void ExactMatch_CaseInsensitive()
    {
        var items = new List<InventoryItem> { Item("PASTA") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("pasta", items)?.ProductName, Is.EqualTo("PASTA"));
    }

    // Diacritic normalisation
    [Test]
    public void Diacritics_JalapenoMatchesJalapeño()
    {
        var items = new List<InventoryItem> { Item("jalapeno") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("jalapeño", items)?.ProductName, Is.EqualTo("jalapeno"));
    }

    [Test]
    public void Diacritics_JalapeñoMatchesJalapeno()
    {
        var items = new List<InventoryItem> { Item("jalapeño") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("jalapeno", items)?.ProductName, Is.EqualTo("jalapeño"));
    }

    // False-positive guard: word boundaries
    [Test]
    public void SubstringFalsePositive_LøgDoesNotMatchHvidløg()
    {
        var items = new List<InventoryItem> { Item("hvidløg") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("løg", items), Is.Null);
    }

    [Test]
    public void SubstringFalsePositive_HvidløgDoesNotMatchLøg()
    {
        var items = new List<InventoryItem> { Item("løg") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("hvidløg", items), Is.Null);
    }

    // Word-level subset matching
    [Test]
    public void WordSubset_OksekødMatchesHakketOksekød()
    {
        var items = new List<InventoryItem> { Item("Hakket oksekød") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("oksekød", items)?.ProductName, Is.EqualTo("Hakket oksekød"));
    }

    [Test]
    public void WordSubset_HakketOksekødMatchesOksekød()
    {
        var items = new List<InventoryItem> { Item("oksekød") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("Hakket oksekød", items)?.ProductName, Is.EqualTo("oksekød"));
    }

    // Generic ingredient must not match a compound branded product (false-positive guard)
    [Test]
    public void WordSubset_SaltDoesNotMatchSaltAndVinegarChips()
    {
        var items = new List<InventoryItem> { Item("Salt and Vinegar Chips") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("salt", items), Is.Null);
    }

    // No match
    [Test]
    public void NoMatch_ReturnsNull()
    {
        var items = new List<InventoryItem> { Item("mælk"), Item("smør") };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("æg", items), Is.Null);
    }

    // Exact match wins over word-subset
    [Test]
    public void ExactMatchPreferredOverWordSubset()
    {
        var løg = Item("løg");
        var hvidløg = Item("hvidløg");
        var items = new List<InventoryItem> { hvidløg, løg };
        Assert.That(RecipeIngredientMatcher.FindBestMatch("løg", items), Is.SameAs(løg));
    }
}