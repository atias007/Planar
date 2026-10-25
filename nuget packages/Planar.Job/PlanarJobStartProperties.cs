using System;

namespace Planar.Job
{
    public class PlanarJobStartProperties
    {
        public static PlanarJobStartProperties Default => new PlanarJobStartProperties();
        public TimeSpan LogFlushTimeout { get; set; } = TimeSpan.FromSeconds(20);
        public string EncryptionKey { get; set; } = string.Empty;
        internal byte[] EncryptionKeyBytes { get; set; } = Array.Empty<byte>();
    }
}