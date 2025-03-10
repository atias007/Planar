﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Planar.Common;
using Planar.Common.Exceptions;
using System;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Startup
{
    public static class AppSettingsInitializer
    {
        private const string encryptedPrefix = "encrypted:";

        public static void Initialize()
        {
            UpgradeToYml();
            UpgradeToYmlVersion2();
            var config = ReadAppSettings();
            EncryptSettings(config);
            Initialize(config);
        }

        private static void EncryptSettings(IConfiguration config)
        {
            try
            {
                var encrypt = AppSettings.GetSettings(config, EnvironmentVariableConsts.EncryptAllSettingsVariableKey, "general", "encrypt all settings", false);
                if (!encrypt) { return; }

                Console.WriteLine("[x] Encrypt all aettings files");
                var path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings);
                var files = Directory.GetFiles(path, "*.yml");

                var cryptographyKey = Environment.GetEnvironmentVariable(Consts.CryptographyKeyVariableKey) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(cryptographyKey))
                {
                    throw new AppSettingsException(
                                               $@"Environment variable {Consts.CryptographyKeyVariableKey} not found.
                        You can create new key with cli command: service create-cryptography-key
                        Then set the key in environment variable (i.e. setx {Consts.CryptographyKeyVariableKey} <generated_key>)");
                }

                var cipher = new Aes256Cipher(cryptographyKey);

                foreach (var file in files)
                {
                    var content = File.ReadAllText(file);
                    if (content.StartsWith(encryptedPrefix)) { continue; }
                    var encrypted = cipher.Encrypt(content);
                    File.WriteAllText(file, $"{encryptedPrefix}{encrypted}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(string.Empty.PadLeft(80, '-'));
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                Console.WriteLine("wait 30 seconds before terminate process");
                Thread.Sleep(30_000);
                Console.ReadLine();
                Environment.Exit(-1);
            }
        }

        private static void Initialize(IConfiguration config)
        {
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

        private static IConfiguration ReadAppSettings()
        {
            var file = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.yml");

            IConfiguration config = null;

            try
            {
                Console.WriteLine("[x] Read AppSettings file");

                using var stream = YmlFileReader.ReadStreamAsync(file).Result;

                config = new ConfigurationBuilder()
                    .AddYamlStream(stream)
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

            return config;
        }

        public static void TestDatabaseConnection()
        {
            AppSettings.TestConnectionString();
        }

        public static void TestDatabasePermission()
        {
            AppSettings.TestDatabasePermission();
        }

        private static void UpgradeToYmlVersion2()
        {
            var ymlFile = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.yml");
            var jsonFileInfo = new FileInfo(ymlFile);
            if (!jsonFileInfo.Exists) { return; }

            var ymlContent = File.ReadAllText(ymlFile);
            if (ymlContent.StartsWith(encryptedPrefix)) { return; }
            var lines = File.ReadAllLines(ymlFile);
            if (Array.Exists(lines, l => l.StartsWith("general:"))) { return; }

            Console.WriteLine("[x] Upgrade AppSettings file to new yml format");
            try
            {
                var serializer = new DeserializerBuilder()
                                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                .Build();
                var oldAppSettings = serializer.Deserialize<OldAppSettings>(ymlContent);
                var sb = new StringBuilder();
                sb.AppendLine("general:");
                sb.AppendLine($"  environment: {oldAppSettings.Environment}");
                sb.AppendLine($"  service name: {oldAppSettings.ServiceName}");
                sb.AppendLine($"  instance id: {oldAppSettings.InstanceId}");
                sb.AppendLine($"  max concurrency: {oldAppSettings.MaxConcurrency}");
                sb.AppendLine($"  job auto stop span: {oldAppSettings.JobAutoStopSpan}");
                sb.AppendLine($"  persist running jobs span: {oldAppSettings.PersistRunningJobsSpan}");
                sb.AppendLine($"  scheduler startup delay: {oldAppSettings.SchedulerStartupDelay}");
                sb.AppendLine($"  http port: {oldAppSettings.HttpPort}");
                sb.AppendLine($"  use https: {oldAppSettings.UseHttps}");
                sb.AppendLine($"  https port: {oldAppSettings.HttpsPort}");
                sb.AppendLine($"  use https redirect: {oldAppSettings.UseHttpsRedirect}");
                sb.AppendLine($"  log level: {oldAppSettings.LogLevel}");
                sb.AppendLine($"  swagger ui: {oldAppSettings.SwaggerUI}");
                sb.AppendLine($"  open api ui: {oldAppSettings.OpenApiUI}");
                sb.AppendLine($"  developer exception page: {oldAppSettings.DeveloperExceptionPage}");
                sb.AppendLine($"  concurrency rate limiting: {oldAppSettings.ConcurrencyRateLimiting}");

                sb.AppendLine();
                sb.AppendLine("database:");
                sb.AppendLine($"  provider: {oldAppSettings.DatabaseProvider}");
                sb.AppendLine($"  connection string: {oldAppSettings.DatabaseConnectionString}");
                sb.AppendLine($"  run migration: {oldAppSettings.RunDatabaseMigration}");

                sb.AppendLine();
                sb.AppendLine("cluster:");
                sb.AppendLine($"  clustering: {oldAppSettings.Clustering}");
                sb.AppendLine($"  port: {oldAppSettings.ClusterPort}");
                sb.AppendLine($"  checkin interval: {oldAppSettings.ClusteringCheckinInterval}");
                sb.AppendLine($"  checkin misfire threshold: {oldAppSettings.ClusteringCheckinMisfireThreshold}");
                sb.AppendLine($"  health check interval: {oldAppSettings.ClusterHealthCheckInterval}");

                sb.AppendLine();
                sb.AppendLine("retention:");
                sb.AppendLine($"  trace retention days: {oldAppSettings.ClearTraceTableOverDays}");
                sb.AppendLine($"  job log retention days: {oldAppSettings.ClearJobLogTableOverDays}");
                sb.AppendLine($"  statistics retention days: {oldAppSettings.ClearStatisticsTablesOverDays}");

                sb.AppendLine();
                sb.AppendLine("authentication:");
                sb.AppendLine($"  mode: {oldAppSettings.AuthenticationMode.SplitWords().ToLower()}");
                sb.AppendLine($"  secret: {oldAppSettings.AuthenticationSecret}");
                sb.AppendLine($"  token expire: {oldAppSettings.AuthenticationTokenExpire}");

                sb.AppendLine();
                sb.AppendLine("smtp:");
                sb.AppendLine("  from address: admin@planar.me");
                sb.AppendLine("  from name: Planar");
                sb.AppendLine("  host: smtp.gmail.com");
                sb.AppendLine("  port: 25");
                sb.AppendLine("  use ssl: false");
                sb.AppendLine("  username: null");
                sb.AppendLine("  password: null");

                File.WriteAllText(ymlFile, sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] WARNING: Upgrade AppSettings file to new yml format fail: {ex.Message}");
            }
        }

        private static void UpgradeToYml()
        {
            var jsonFile = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.json");
            var jsonFileInfo = new FileInfo(jsonFile);
            if (!jsonFileInfo.Exists) { return; }

            Console.WriteLine("[x] Upgrade AppSettings file from json to yml format");
            try
            {
                var json = File.ReadAllText(jsonFile);
                var expConverter = new ExpandoObjectConverter();
                dynamic deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(json, expConverter);
                var serializer = new SerializerBuilder()
                                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                .Build();

                var yml = serializer.Serialize(deserializedObject);
                var ymlFile = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.yml");
                File.WriteAllText(ymlFile, yml);
                File.Delete(jsonFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] WARNING: Upgrade AppSettings file from json to yml format fail: {ex.Message}");
            }
        }
    }
}