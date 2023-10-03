using Planar.Startup;
using Serilog;
using System;

namespace Planar
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            DataFolderInitializer.CreateFolderAndFiles();
            AppSettingsInitializer.Initialize();
            WorkingHoursInitializer.Initialize();
            DatabaseMigrationInitializer.RunMigration();
            AppSettingsInitializer.TestDatabaseConnection();
            AppSettingsInitializer.TestDatabasePermission();
            var app = WebApplicationInitializer.Initialize(args);
            CalendarsInitializer.Initialize(app.Services);
            WebApplicationInitializer.Configure(app);
            ContentInitializer.MapContent(app);
            SerilogInitializer.ConfigureSelfLog();
            app.Run();
        }
    }
}