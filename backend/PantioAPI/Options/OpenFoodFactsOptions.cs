namespace PantioAPI.Options;

public class OpenFoodFactsOptions
{
    public const string Section = "OpenFoodFacts";
    public string BaseUrl { get; set; } = "https://world.openfoodfacts.org/";
    public string ContributorUserId { get; set; } = "";
    public string ContributorPassword { get; set; } = "";
}
