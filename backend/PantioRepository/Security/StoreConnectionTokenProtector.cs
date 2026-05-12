using System.Security.Cryptography;
using System.Text;

namespace PantioRepository.Security;

public class StoreConnectionTokenProtector
{
    private const string Prefix = "v1";
    private const char Separator = ':';
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public StoreConnectionTokenProtector(StoreConnectionTokenEncryptionOptions options)
    {
        _key = DecodeKey(options.Key);
    }

    public bool IsEncrypted(string? value)
    {
        return value?.StartsWith($"{Prefix}{Separator}", StringComparison.Ordinal) == true;
    }

    public string? Encrypt(string? value)
    {
        if (string.IsNullOrEmpty(value) || IsEncrypted(value)) return value;

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return string.Join(
            Separator,
            Prefix,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext));
    }

    public string? Decrypt(string? value)
    {
        if (string.IsNullOrEmpty(value) || !IsEncrypted(value)) return value;

        var parts = value.Split(Separator);
        if (parts.Length != 4 || parts[0] != Prefix) throw new CryptographicException("Encrypted store connection token has an invalid format.");

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DecodeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("TokenEncryption:Key must be configured.");

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("TokenEncryption:Key must be a base64-encoded 32-byte key.", ex);
        }

        if (decoded.Length != KeySizeBytes) throw new InvalidOperationException("TokenEncryption:Key must decode to exactly 32 bytes.");

        return decoded;
    }
}
