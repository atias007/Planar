using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Planar.Common;
using Planar.Service;
using Planar.Service.Exceptions;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Planar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
            var app = CreateHostBuilder(args).Build();
            app.Run();
            Global.Clear();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var result = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    InitializeAppSettings();

                    webBuilder
                        .UseKestrel(options =>
                        {
                            options.Listen(IPAddress.Loopback, 0);
                            options.ListenAnyIP(AppSettings.HttpPort);
                            options.ListenAnyIP(9999, x => x.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
                            if (AppSettings.UseHttps)
                            {
                                options.ListenAnyIP(AppSettings.HttpsPort, opts => opts.UseHttps());
                            }
                        })
                        .UseStartup<Startup>()

                        ////webBuilder.UseKestrel(opts =>
                        ////{
                        ////    opts.ListenLocalhost(AppSettings.HttpPort);
                        ////    if (AppSettings.HttpsPort > 0)
                        ////    {
                        ////        opts.ListenLocalhost(AppSettings.HttpsPort, opts => opts.UseHttps());
                        ////    }
                        ////    else
                        ////    {
                        ////        // TODO: add warning log
                        ////    }
                        ////});

                        .ConfigureAppConfiguration(builder =>
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

        private static void InitializeAppSettings()
        {
            var file1 = Path.Combine(FolderConsts.Data, FolderConsts.Settings, "appsettings.json");
            var file2 = Path.Combine(FolderConsts.Data, FolderConsts.Settings, $"appsettings.{Global.Environment}.json");

            var configBuilder = new ConfigurationBuilder();
            configBuilder
                .AddJsonFile(file1, false, true)
                .AddJsonFile(file2, true, true)
                .AddEnvironmentVariables();

            try
            {
                AppSettings.Initialize(configBuilder.Build());
            }
            catch (AppSettingsException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                Environment.Exit(-1);
            }
        }
    }
}