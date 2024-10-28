namespace Planar.Common;

public enum DbProviders
{
    Unknown,
    SqlServer,
    Sqlite
}

public class DatabaseSettings
{
    public string? ConnectionString { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DbProviders ProviderName { get; set; }
    public bool RunMigration { get; set; }

    public bool ProviderHasPermissions { get; set; }
}