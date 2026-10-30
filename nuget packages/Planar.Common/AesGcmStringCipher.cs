using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Planar.Job
{
    /// <summary>
    /// .NET Standard 2.0 port of the original .NET 10 AES-GCM helper.
    /// System.Security.Cryptography.AesGcm does not exist on netstandard2.0,
    /// so GCM is provided by BouncyCastle. The wire format is unchanged:
    /// base64( nonce[12] || tag[16] || ciphertext ), so payloads stay
    /// interchangeable with the .NET 10 implementation.
    ///
    /// Requires NuGet package: BouncyCastle.Cryptography (>= 2.0)
    /// </summary>
    internal static class AesGcmStringCipher
    {
        private const int NonceSize = 12; // 96-bit nonce is the GCM standard
        private const int TagSize = 16;   // 128-bit auth tag
        private const int TagSizeInBits = TagSize * 8;

        public static string Encrypt(string plainText, byte[] key)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }
            if (key == null) { throw new ArgumentNullException(nameof(key)); }

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] nonce = GetRandomBytes(NonceSize); // MUST be unique per (key, message)

            GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key), TagSizeInBits, nonce));

            // BouncyCastle writes ciphertext and tag into a single buffer: ciphertext || tag
            byte[] output = new byte[cipher.GetOutputSize(plainBytes.Length)];
            int written = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, output, 0);
            written += cipher.DoFinal(output, written);

            int cipherLength = written - TagSize; // == plainBytes.Length for GCM (stream cipher core)

            // Repack as nonce + tag + ciphertext so Decrypt is self-contained.
            byte[] result = new byte[NonceSize + TagSize + cipherLength];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(output, cipherLength, result, NonceSize, TagSize);             // tag
            Buffer.BlockCopy(output, 0, result, NonceSize + TagSize, cipherLength);         // ciphertext

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherTextBase64, byte[] key)
        {
            if (cipherTextBase64 == null) { throw new ArgumentNullException(nameof(cipherTextBase64)); }
            if (key == null) { throw new ArgumentNullException(nameof(key)); }

            byte[] data = Convert.FromBase64String(cipherTextBase64);

            if (data.Length < NonceSize + TagSize)
                throw new ArgumentException("Ciphertext is too short / malformed.", nameof(cipherTextBase64));

            byte[] nonce = new byte[NonceSize];
            Buffer.BlockCopy(data, 0, nonce, 0, NonceSize);

            int cipherLength = data.Length - NonceSize - TagSize;

            // BouncyCastle expects the tag appended to the ciphertext.
            byte[] input = new byte[cipherLength + TagSize];
            Buffer.BlockCopy(data, NonceSize + TagSize, input, 0, cipherLength);            // ciphertext
            Buffer.BlockCopy(data, NonceSize, input, cipherLength, TagSize);                // tag

            GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), TagSizeInBits, nonce));

            byte[] plainBytes = new byte[cipher.GetOutputSize(input.Length)];

            int written;
            try
            {
                written = cipher.ProcessBytes(input, 0, input.Length, plainBytes, 0);
                written += cipher.DoFinal(plainBytes, written);
            }
            catch (InvalidCipherTextException ex)
            {
                // Preserve the original contract: tampering / wrong key surfaces as CryptographicException.
                throw new CryptographicException("Authentication tag mismatch — data was tampered with or the key is wrong.", ex);
            }

            return Encoding.UTF8.GetString(plainBytes, 0, written);
        }

        /// <summary>Generate a fresh 256-bit key.</summary>
        public static byte[] NewKey()
        {
            return GetRandomBytes(32);
        }

        // RandomNumberGenerator.GetBytes(int) is a .NET 6+ static; netstandard2.0 needs an instance.
        private static byte[] GetRandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }
    }
}