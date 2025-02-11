namespace Planar.Client.Entities
{
    public class KeyValueItem
    {
        public string Key { get; set; } = string.Empty;
#if NETSTANDARD2_0
        public string Value { get; set; }
#else
        public string? Value { get; set; }
#endif
    }
}