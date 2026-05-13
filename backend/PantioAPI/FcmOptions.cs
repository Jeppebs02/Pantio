namespace PantioAPI;

public class FcmOptions
{
    public const string Section = "Firebase";
    public string ProjectId { get; set; } = "";
    public string ServiceAccountPath { get; set; } = "firebase-service-account.json";
}
