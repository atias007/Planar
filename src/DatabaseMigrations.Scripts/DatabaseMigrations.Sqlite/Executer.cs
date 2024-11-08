using DbUp;
using DbUp.Engine;
using System.Data;
using System.Reflection;

namespace DatabaseMigrations.Sqlite;

public class Executer : IExecuter
{
    public static Assembly ScriptAssembly => typeof(Executer).Assembly;

    public void EnsureDatabaseExists(string connectionString)
    {
        // *** DO NOTHING ***
    }

    public DatabaseUpgradeResult DemoExecute(string connectionString)
    {
        var builder =
           DeployChanges.To
               .SQLiteDatabase(connectionString)
               .WithScriptsEmbeddedInAssembly(ScriptAssembly)
               .LogToConsole()
               .LogScriptOutput()
               .WithTransactionAlwaysRollback();

        var upgrader = builder.Build();
        var result = upgrader.PerformUpgrade();
        return result;
    }

    public DatabaseUpgradeResult Execute(string connectionString)
    {
        var builder =
           DeployChanges.To
               .SQLiteDatabase(connectionString)
               .WithScriptsEmbeddedInAssembly(ScriptAssembly)
               .LogToConsole()
               .LogScriptOutput()
               .WithTransaction();

        var upgrader = builder.Build();
        var result = upgrader.PerformUpgrade();
        return result;
    }

    public IEnumerable<string> GetScripts(string connectionString)
    {
        var builder =
            DeployChanges.To
                .SQLiteDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(ScriptAssembly)
                .LogToConsole()
                .LogScriptOutput();

        var result = builder.Build().GetScriptsToExecute().Select(s => s.Name);
        return result;
    }
}