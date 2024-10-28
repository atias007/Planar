using DbUp;
using Planar.Common;
using Planar.Common.Exceptions;
using System;
using System.Linq;

namespace Planar.Startup;

public static class DatabaseMigrationInitializer
{
    private static readonly IExecuter _executer = DbFactory.CreateDbMigrationExecuter(AppSettings.Database.ProviderName);

    private static void HandleError(Exception ex)
    {
        Console.WriteLine(string.Empty.PadLeft(80, '-'));
        Console.WriteLine("Migration fail");
        Console.WriteLine(string.Empty.PadLeft(80, '-'));
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex);
        Console.ResetColor();

        Console.ReadLine();
        Environment.Exit(-1);
    }

    public static void RunMigration()
    {
        if (!AppSettings.Database.RunMigration)
        {
            Console.WriteLine("[x] Skip database migration");
            var count = CountScriptsToExecute();
            if (count > 0)
            {
                throw new PlanarException($"there are {count} script in database migration to run. enable database migrations in settings file or run database migrations manually");
            }

            return;
        }

        try
        {
            Console.WriteLine("[x] Run database migration");

            _executer.EnsureDatabaseExists(AppSettings.Database.ConnectionString);

            var count = CountScriptsToExecute();
            Console.WriteLine($"    - Found {count} scripts to run");

            if (count == 0)
            {
                return;
            }

            Console.WriteLine(string.Empty.PadLeft(80, '-'));
            var result = _executer.Execute(AppSettings.Database.ConnectionString);

            if (result.Successful)
            {
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                Console.WriteLine("Migration success");
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
            }
            else
            {
                HandleError(result.Error);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private static int CountScriptsToExecute()
    {
        var list = _executer.GetScripts(AppSettings.Database.ConnectionString);
        return list.Count();
    }
}