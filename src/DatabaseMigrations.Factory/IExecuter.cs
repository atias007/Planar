using DbUp.Engine;

namespace DatabaseMigrations.Factory;

public interface IExecuter
{
    DatabaseUpgradeResult DemoExecute(string connectionString);

    void EnsureDatabaseExists(string connectionString);

    DatabaseUpgradeResult Execute(string connectionString);

    IEnumerable<string> GetScripts(string connectionString);
}