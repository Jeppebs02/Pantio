namespace PantioAPI;

public class FcmOptions
{
    public const string Section = "Firebase";
    public string ProjectId { get; set; } = string.Empty;
    public string Type { get; set; } = "service_account";
    public string PrivateKeyId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string AuthUri { get; set; } = "https://accounts.google.com/o/oauth2/auth";
    public string TokenUri { get; set; } = "https://oauth2.googleapis.com/token";
    public string AuthProviderCertUrl { get; set; } = "https://www.googleapis.com/oauth2/v1/certs";
    public string ClientCertUrl { get; set; } = string.Empty;
    public string UniverseDomain { get; set; } = "googleapis.com";
}
