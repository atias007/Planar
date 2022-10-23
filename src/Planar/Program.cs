using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Planar.Common;
using Planar.Service;
using Planar.Service.Exceptions;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
            CreateDataFoldersAndFiles();
            InitializeAppSettings();
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
                    webBuilder
                        .UseKestrel(options =>
                        {
                            //options.Listen(IPAddress.Loopback, 0);
                            options.ListenAnyIP(AppSettings.HttpPort);
                            if (AppSettings.Clustering)
                            {
                                options.ListenAnyIP(AppSettings.HttpPort + 10000, x => x.Protocols = HttpProtocols.Http2);
                            }

                            if (AppSettings.UseHttps)
                            {
                                options.ListenAnyIP(AppSettings.HttpsPort, opts => opts.UseHttps());
                            }
                        })
                        .UseStartup<Startup>()
                        .ConfigureAppConfiguration(builder =>
                        {
                            Console.WriteLine("[x] Load configuration & app settings");
                            var file1 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.json");
                            var file2 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, $"AppSettings.{Global.Environment}.json");

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
            var file1 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.json");
            var file2 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, $"AppSettings.{Global.Environment}.json");

            IConfiguration config = null;

            try
            {
                Console.WriteLine("[x] Read AppSettings files");

                config = new ConfigurationBuilder()
                    .AddJsonFile(file1, optional: false, reloadOnChange: true)
                    .AddJsonFile(file2, optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fail to read application settings:");
                Console.WriteLine(ex.Message);
                Thread.Sleep(60000);
                Console.ReadLine();
                Environment.Exit(-1);
            }

            try
            {
                AppSettings.Initialize(config);
            }
            catch (AppSettingsException ex)
            {
                Console.WriteLine("Fail to initialize application settings:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Thread.Sleep(60000);
                Console.ReadLine();
                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                Console.WriteLine(ex.ToString());
                Thread.Sleep(60000);
                Console.ReadLine();
                Environment.Exit(-1);
            }
        }

        private static void CreateDataFoldersAndFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resources =
                assembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith($"{nameof(Planar)}.Data"))
                .Select(r => new { ResourceName = r, FileInfo = ConvertResourceToPath(r) })
                .ToList();

            // create folders
            resources.ForEach(r =>
            {
                if (!r.FileInfo.Directory.Exists)
                {
                    r.FileInfo.Directory.Create();
                }
            });

            // create files
            Parallel.ForEach(resources, async source =>
            {
#if DEBUG
                if (source.FileInfo.Exists)
                {
                    source.FileInfo.Delete();
                }
#endif

                if (!source.FileInfo.Exists)
                {
                    using var stream = assembly.GetManifestResourceStream(source.ResourceName);
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    await File.WriteAllTextAsync(source.FileInfo.FullName, content);
                }
            });
        }

        private static readonly string[] sufixes = new[] { "json", "md", "ps1", "yml" };

        private static FileInfo ConvertResourceToPath(string resource)
        {
            var parts = resource.Split('.');
            parts = parts[1..];

            var sufix = parts.Last();
            if (sufixes.Contains(sufix))
            {
                parts = parts[..^1];
                var last = parts.Last();
                parts[^1] = $"{last}.{sufix}";
            }

            var path = Path.Combine(parts);
            var result = FolderConsts.GetPath(path);
            return new FileInfo(result);
        }
    }
}