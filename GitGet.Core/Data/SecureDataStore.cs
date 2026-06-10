using System.Security.Cryptography;
using System.Text;
using GitGet.Core.Interfaces;

namespace GitGet.Core.Data;

public class SecureDataStore : ISecureDataStore
{
    private readonly string _storagePath;

    public SecureDataStore(string storagePath)
    {
        _storagePath = storagePath;
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task SaveTokenAsync(string key, string token, CancellationToken ct = default)
    {
        var encrypted = await EncryptAsync(token);
        var filePath = GetFilePath(key);
        await File.WriteAllTextAsync(filePath, Convert.ToBase64String(encrypted), ct);
    }

    public async Task<string?> GetTokenAsync(string key, CancellationToken ct = default)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var base64 = await File.ReadAllTextAsync(filePath, ct);
            var encrypted = Convert.FromBase64String(base64);
            return await DecryptAsync(encrypted);
        }
        catch
        {
            return null;
        }
    }

    public Task ClearTokenAsync(string key, CancellationToken ct = default)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        var sanitizedKey = key.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
        return Path.Combine(_storagePath, $"{sanitizedKey}.enc");
    }

    private static async Task<byte[]> EncryptAsync(string plainText)
    {
        // Use AES-256-GCM for encryption
        var key = GetOrCreateKey();
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // Combine: nonce + tag + ciphertext
        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return result;
    }

    private static async Task<string> DecryptAsync(byte[] encryptedData)
    {
        var key = GetOrCreateKey();
        var nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
        var tagSize = AesGcm.TagByteSizes.MaxSize; // 16

        var nonce = encryptedData[..nonceSize];
        var tag = encryptedData[nonceSize..(nonceSize + tagSize)];
        var ciphertext = encryptedData[(nonceSize + tagSize)..];

        var plainBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, ciphertext, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] GetOrCreateKey()
    {
        var keyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GitGet", ".enc_key");

        var dir = Path.GetDirectoryName(keyPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(keyPath))
            return File.ReadAllBytes(keyPath);

        var key = new byte[32]; // AES-256
        RandomNumberGenerator.Fill(key);
        File.WriteAllBytes(keyPath, key);
        return key;
    }
}