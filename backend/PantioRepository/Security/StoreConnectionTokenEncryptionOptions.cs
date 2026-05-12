namespace PantioRepository.Security;

public class StoreConnectionTokenEncryptionOptions
{
    public const string Section = "TokenEncryption";

    public string Key { get; set; } = string.Empty;
}
