using DatabaseMigrations;
using Planar.Common;
using Planar.Common.Exceptions;
using System;
using System.Linq;

namespace Planar.Startup
{
    public static class DatabaseMigrationInitializer
    {
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
            if (!AppSettings.RunDatabaseMigration)
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

                Runner.EnsureDatabaseExists(AppSettings.DatabaseConnectionString);

                var count = CountScriptsToExecute();
                Console.WriteLine($"    - Found {count} scripts to run");

                if (count == 0)
                {
                    return;
                }

                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                var result = Runner.Execute(AppSettings.DatabaseConnectionString);

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
            var list = Runner.GetScripts(AppSettings.DatabaseConnectionString);
            return list.Count();
        }
    }
}