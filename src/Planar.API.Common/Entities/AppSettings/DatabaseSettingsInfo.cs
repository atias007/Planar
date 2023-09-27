namespace Planar.API.Common.Entities
{
    public class DatabaseSettingsInfo
    {
        public string? ConnectionString { get; set; }
        public string? Provider { get; set; }
        public bool RunMigration { get; set; }
    }
}