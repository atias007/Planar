using JKang.IpcServiceFramework.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Planner.API.Common;
using Planner.Common;
using Planner.Service;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace Planner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
            CreateHostBuilder(args).Build().Run();
            Global.Clear();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var result = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureIpcHost(builder =>
                {
                    // configure IPC endpoints
                    builder.AddNamedPipeEndpoint<IPlannerCommand>(pipeName: "pipeinternal");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.UseUrls("http://localhost:2306", "https://localhost:2307");

                    webBuilder.ConfigureAppConfiguration(builder =>
                    {
                        var file1 = Path.Combine(FolderConsts.Data, FolderConsts.Settings, "appsettings.json");
                        var file2 = Path.Combine(FolderConsts.Data, FolderConsts.Settings, $"appsettings.{Global.Environment}.json");

                        builder
                        .AddJsonFile(file1, false, true)
                        .AddJsonFile(file2, true, true)
                        .AddCommandLine(args)
                        .AddEnvironmentVariables();
                    });

                    Serilog.Debugging.SelfLog.Enable(msg =>
                    {
                        Debug.Print(msg);
                        Debugger.Break();
                    });
                })
                .UseSerilog((context, config) => ConfigureSerilog(config));

            return result;
        }

        private static void ConfigureSerilog(LoggerConfiguration loggerConfig)
        {
            var file = Path.Combine(FolderConsts.Data, FolderConsts.Settings, "Serilog.json");
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
                    if (connSection != null)
                    {
                        if (string.IsNullOrEmpty(connSection.Value))
                        {
                            connSection.Value = AppSettings.DatabaseConnectionString;
                        }
                    }
                }
            }

            loggerConfig.ReadFrom.Configuration(configuration);
        }
    }
}