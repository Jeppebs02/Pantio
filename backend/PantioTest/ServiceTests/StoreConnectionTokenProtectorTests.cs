using System.Security.Cryptography;
using PantioRepository.Security;

namespace PantioTest.ServiceTests;

public class StoreConnectionTokenProtectorTests
{
    [Test]
    [Category("S-04")]
    public void Encrypt_ProducesDecryptableCiphertext()
    {
        var protector = CreateProtector();

        var encrypted = protector.Encrypt("access-token");

        Assert.That(encrypted, Is.Not.EqualTo("access-token"));
        Assert.That(protector.IsEncrypted(encrypted), Is.True);
        Assert.That(protector.Decrypt(encrypted), Is.EqualTo("access-token"));
    }

    [Test]
    [Category("S-04")]
    public void Encrypt_UsesUniqueNonceForSamePlaintext()
    {
        var protector = CreateProtector();

        var first = protector.Encrypt("refresh-token");
        var second = protector.Encrypt("refresh-token");

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    [Category("S-04")]
    public void Encrypt_AndDecrypt_PreserveNullAndEmptyValues()
    {
        var protector = CreateProtector();

        Assert.That(protector.Encrypt(null), Is.Null);
        Assert.That(protector.Decrypt(null), Is.Null);
        Assert.That(protector.Encrypt(string.Empty), Is.EqualTo(string.Empty));
        Assert.That(protector.Decrypt(string.Empty), Is.EqualTo(string.Empty));
    }

    [Test]
    [Category("S-04")]
    public void Decrypt_ReturnsLegacyPlaintextValue()
    {
        var protector = CreateProtector();

        Assert.That(protector.Decrypt("legacy-plaintext-token"), Is.EqualTo("legacy-plaintext-token"));
    }

    [Test]
    [Category("S-04")]
    public void Constructor_RejectsInvalidKey()
    {
        var options = new StoreConnectionTokenEncryptionOptions { Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)) };

        var exception = Assert.Throws<InvalidOperationException>(() => new StoreConnectionTokenProtector(options));

        Assert.That(exception!.Message, Does.Contain("32 bytes"));
    }

    private static StoreConnectionTokenProtector CreateProtector()
    {
        return new StoreConnectionTokenProtector(new StoreConnectionTokenEncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });
    }
}
