using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Planar.Service;
using Serilog;
using Serilog.Debugging;
using System;
using System.Diagnostics;
using Planar.Startup.Logging;
using Planar.Common;
using Serilog.Sinks.MSSqlServer;

namespace Planar.Startup
{
    public static class SerilogInitializer
    {
        public static void ConfigureSelfLog()
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

            var file = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "Serilog.yml");
            var configuration = new ConfigurationBuilder()
                        .AddYamlFile(file)
                        .AddEnvironmentVariables()
                        .Build();

            var sqlSink = new MSSqlServerSinkOptions
            {
                TableName = "Trace",
                AutoCreateSqlTable = false,
                SchemaName = "dbo",
            };

            var sqlColumns = new ColumnOptions();
            sqlColumns.Store.Remove(StandardColumn.MessageTemplate);
            sqlColumns.Store.Remove(StandardColumn.Properties);
            sqlColumns.Store.Add(StandardColumn.LogEvent);
            sqlColumns.LogEvent.ExcludeStandardColumns = true;

            config.ReadFrom.Configuration(configuration);
            config.WriteTo.MSSqlServer(
                connectionString: AppSettings.DatabaseConnectionString,
                sinkOptions: sqlSink,
                columnOptions: sqlColumns);

            config.Enrich.WithPlanarEnricher();
            config.Enrich.FromGlobalLogContext();
            config.Filter.WithPlanarFilter();

            SelfLog.Enable(Console.Out);
        }
    }
}