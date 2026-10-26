using Org.BouncyCastle.Utilities;
using System;

namespace Planar.Job
{
    public abstract class BaseJobStartPropertiesBuilder
    {
        public static void WithEncryptionKey(PlanarJobStartProperties properties, string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey)) { throw new ArgumentNullException(nameof(encryptionKey)); }
            properties.EncryptionKey = encryptionKey;
            properties.EncryptionKeyBytes = Convert.FromBase64String(encryptionKey);
            if (properties.EncryptionKeyBytes.Length != 32)
            {
                throw new ArgumentException($"Encryption key must be 32 bytes base64 encoding string. current length is {properties.EncryptionKeyBytes.Length}", nameof(encryptionKey));
            }
        }

        public static void WithLogFlushTimeoutSeconds(PlanarJobStartProperties properties, int seconds)
        {
            if (seconds <= 0) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(seconds)); }
            properties.LogFlushTimeout = TimeSpan.FromSeconds(seconds);
        }

        public static void WithLogFlushTimeoutMinutes(PlanarJobStartProperties properties, int minutes)
        {
            if (minutes <= 0) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(minutes)); }
            properties.LogFlushTimeout = TimeSpan.FromMinutes(minutes);
        }

        public static void WithLogFlushTimeout(PlanarJobStartProperties properties, TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero) { throw new ArgumentException("Log flush timeout must be greater than zero", nameof(timeout)); }
            properties.LogFlushTimeout = timeout;
        }
    }
}