namespace Planar.Common;

public enum DbProviders
{
    Unknown,
    SqlServer,
    Sqlite
}

public class DatabaseSettings
{
    public string? ConnectionString { get; internal set; }
    public string Provider { get; internal set; } = string.Empty;
    public DbProviders ProviderName { get; internal set; }
    public bool RunMigration { get; internal set; }
    public bool ProviderHasPermissions { get; internal set; }
    public bool ProviderAllowClustering { get; internal set; }
}