using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Planar.Service;
using Serilog;
using Serilog.Debugging;
using System;
using System.Diagnostics;

namespace Planar.Startup
{
    public static class SerilogInitializer
    {
        public static void InitializeSelfLog()
        {
            SelfLog.Enable(msg =>
            {
                Console.WriteLine(msg);
                Debugger.Break();
            });
        }

        public static void Configure(HostBuilderContext context, LoggerConfiguration config)
        {
            Console.WriteLine("[x] Configure serilog");
            var file = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "Serilog.json");
            var configuration = new ConfigurationBuilder()
                        .AddJsonFile(file)
                        .AddEnvironmentVariables()
                        .Build();

            var sections = configuration.GetSection("Serilog:WriteTo").GetChildren();
            foreach (var item in sections)
            {
                var name = item.GetValue<string>("Name");
                if (name == "MSSqlServer")
                {
                    var connSection = item.GetSection("Args:connectionString");
                    if (connSection != null && string.IsNullOrEmpty(connSection.Value))
                    {
                        if (string.IsNullOrEmpty(connSection.Value))
                        {
                            connSection.Value = AppSettings.DatabaseConnectionString;
                        }
                    }
                }
            }

            config.ReadFrom.Configuration(configuration);
        }
    }
}