using System;

namespace Planar.Common
{
    public class PlanarJobStartProperties
    {
        public static PlanarJobStartProperties Default => new PlanarJobStartProperties();
        public TimeSpan LogFlushTimeout { get; set; } = TimeSpan.FromSeconds(20);
    }
}