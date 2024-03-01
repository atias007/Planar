namespace Planar.Client.Entities
{
    public partial class GlobalConfig
    {
        public string Key { get; set; } = null!;

        public string? Value { get; set; }

        public string Type { get; set; } = null!;
    }
}