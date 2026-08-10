using Microsoft.IdentityModel.Tokens; // only needed for the SymmetricSecurityKey bridge
using System;
using System.Security.Cryptography;
using System.Text;

namespace Planar.Common.Helpers;

/// <summary>
/// Authenticated string encryption using AES-GCM.
/// Output layout (base64): [nonce(12) | tag(16) | ciphertext].
/// </summary>
public static class AesGcmStringCipher
{
    private const int NonceSize = 12; // 96-bit nonce is the GCM standard
    private const int TagSize = 16;   // 128-bit auth tag

    public static string Encrypt(string plainText, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize); // MUST be unique per (key, message)
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Pack nonce + tag + ciphertext so Decrypt is self-contained.
        var result = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherTextBase64, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(cipherTextBase64);
        var data = Convert.FromBase64String(cipherTextBase64);

        if (data.Length < NonceSize + TagSize)
            throw new ArgumentException("Ciphertext is too short / malformed.", nameof(cipherTextBase64));

        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var cipherBytes = data.AsSpan(NonceSize + TagSize);

        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        // Throws CryptographicException if the tag doesn't verify (tampering / wrong key).
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>Generate a fresh 256-bit key.</summary>
    public static byte[] NewKey() => RandomNumberGenerator.GetBytes(32);

    /// <summary>Bridge: reuse the raw bytes of an existing SymmetricSecurityKey.</summary>
    public static byte[] FromSecurityKey(SymmetricSecurityKey key) => key.Key;
}