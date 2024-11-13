using DbUp.Engine;
using System.Collections.Generic;

namespace DbUp;

public interface IExecuter
{
    DatabaseUpgradeResult DemoExecute(string connectionString);

    void EnsureDatabaseExists(string connectionString);

    DatabaseUpgradeResult Execute(string connectionString);

    IEnumerable<string> GetScripts(string connectionString);
}