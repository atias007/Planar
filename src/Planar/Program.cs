using Planar.Startup;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Planar
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            DataFolderInitializer.CreateFolderAndFiles();
            AppSettingsInitializer.Initialize();
            WorkingHoursInitializer.Initialize();
            DatabaseMigrationInitializer.RunMigration();
            AppSettingsInitializer.TestDatabaseConnection();
            AppSettingsInitializer.TestDatabasePermission();
            var app = WebApplicationInitializer.Initialize(args);
            await DatabaseMigrationInitializer.FixJobProperties(app.Services);
            CalendarsInitializer.Initialize(app.Services);
            WebApplicationInitializer.Configure(app);
            ContentInitializer.MapContent(app);
            SerilogInitializer.ConfigureSelfLog();
            await app.RunAsync();
        }
    }
}