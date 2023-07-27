using DbUp;
using DbUp.Engine;
using System.Data;
using System.Reflection;

namespace DatabaseMigrations
{
    public static class Runner
    {
        public static Assembly ScriptAssembly => typeof(Runner).Assembly;

        public static void EnsureDatabaseExists(string connectionString)
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }

        public static DatabaseUpgradeResult DemoExecute(string connectionString)
        {
            var builder =
               DeployChanges.To
                   .SqlDatabase(connectionString)
                   .WithScriptsEmbeddedInAssembly(ScriptAssembly)
                   .LogToConsole()
                   .LogScriptOutput()
                   .WithTransactionAlwaysRollback();

            var upgrader = builder.Build();
            var result = upgrader.PerformUpgrade();
            return result;
        }

        public static DatabaseUpgradeResult Execute(string connectionString)
        {
            var builder =
               DeployChanges.To
                   .SqlDatabase(connectionString)
                   .WithScriptsEmbeddedInAssembly(ScriptAssembly)
                   .LogToConsole()
                   .LogScriptOutput()
                   .WithTransaction();

            var upgrader = builder.Build();
            var result = upgrader.PerformUpgrade();
            return result;
        }

        public static IEnumerable<string> GetScripts(string connectionString)
        {
            var builder =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(ScriptAssembly)
                    .LogToConsole()
                    .LogScriptOutput();

            var result = builder.Build().GetScriptsToExecute().Select(s => s.Name);
            return result;
        }
    }
}