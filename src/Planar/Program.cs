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
            DatabaseMigrationInitializer.RunMigration();
            var app = WebApplicationInitializer.Initialize(args);
            WebApplicationInitializer.Configure(app);
            ContentInitializer.MapContent(app);
            SerilogInitializer.ConfigureSelfLog();

            app.Run();
        }
    }
}