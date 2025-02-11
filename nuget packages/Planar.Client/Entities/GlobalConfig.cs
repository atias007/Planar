namespace Planar.Client.Entities
{
    public partial class GlobalConfig
    {
#if NETSTANDARD2_0
        public string Key { get; set; }

        public string Value { get; set; }

        public string Type { get; set; }
#else
        public string Key { get; set; } = null!;

        public string? Value { get; set; }

        public string Type { get; set; } = null!;
#endif
    }
}