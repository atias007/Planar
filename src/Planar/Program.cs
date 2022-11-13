using Planar.Common;
using Planar.Startup;
using Serilog;
using System;

namespace Planar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            DataFolderInitializer.CreateFolderAndFiles();
            AppSettingsInitializer.Initialize();
            var app = WebApplicationInitializer.Initialize(args);
            WebApplicationInitializer.Configure(app);
            ContentInitializer.MapContent(app);
            app.Run();
            Global.Clear();
        }
    }
}