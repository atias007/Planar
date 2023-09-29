namespace Planar.Common
{
    public class DatabaseSettings
    {
        public string? ConnectionString { get; set; }
        public string Provider { get; set; } = string.Empty;
        public bool RunMigration { get; set; }
    }
}