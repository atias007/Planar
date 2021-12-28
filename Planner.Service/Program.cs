using JKang.IpcServiceFramework.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planner.API.Common;
using Planner.Common;
using Planner.Service.API;
using Planner.Service.Data;
using Quartz;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Planner.Service
{
    internal class Program
    {
        public static IHost CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                        .AddJsonFile(@"Settings\appsettings.json", true, true)
                        .AddJsonFile(@$"Settings\appsettings.{Consts.EnvironmentVariableKey}.json", true, true)
                        .AddCommandLine(args)
                        .AddEnvironmentVariables()
                        .Build();

            var builder = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureIpcHost(builder =>
                {
                    // configure IPC endpoints
                    builder.AddNamedPipeEndpoint<IPlannerCommand>(pipeName: "pipeinternal");
                })
                .ConfigureServices(services =>
                {
                    Serilog.Debugging.SelfLog.Enable(msg =>
                    {
                        Debug.Print(msg);
                        Debugger.Break();
                    });

                    AppSettings.Initialize(configuration);

                    services.AddDbContext<PlannerContext>(o => o.UseSqlServer(AppSettings.DatabaseConnectionString), ServiceLifetime.Transient)
                        .AddSingleton<IConfiguration>(configuration)
                        .AddTransient<DataLayer>()
                        .AddTransient<DeamonBL>()
                        .AddTransient<MainService>()
                        .AddScoped<IPlannerCommand, DeamonService>()
                        .AddHostedService<MainService>()
                        .AddHostedService<PersistDataService>();
                })
                .UseSerilog((context, config) => ConfigureSerilog(config));

            return builder.Build();
        }

        private static void ConfigureSerilog(LoggerConfiguration loggerConfig)
        {
            var configuration = new ConfigurationBuilder()
                        .AddJsonFile(@"Settings\Serilog.json")
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

        private static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            var hostTask = Task.Run(() => CreateHostBuilder(args).Run());
            await hostTask;
            Global.Clear();
        }
    }
}