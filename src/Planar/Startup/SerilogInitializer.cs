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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Dynamic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

#pragma warning disable IDE0060 // Remove unused parameter

        public static void Configure(HostBuilderContext context, LoggerConfiguration config)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            UpgradeToYml();
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

        private static void UpgradeToYml()
        {
            var jsonFile = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "Serilog.json");
            var jsonFileInfo = new FileInfo(jsonFile);
            if (!jsonFileInfo.Exists) { return; }

            Console.WriteLine("[x] Upgrade Serilog settings file from json to yml format");
            try
            {
                var json = File.ReadAllText(jsonFile);
                var expConverter = new ExpandoObjectConverter();
                dynamic deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(json, expConverter);
                var serializer = new SerializerBuilder()
                                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                .Build();

                var yml = serializer.Serialize(deserializedObject);
                var ymlFile = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "Serilog.yml");
                File.WriteAllText(ymlFile, yml);
                File.Delete(jsonFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] WARNING: Upgrade Serilog settings file from json to yml format fail: {ex.Message}");
            }
        }
    }
}