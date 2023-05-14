using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Planar.Common;
using Planar.Common.Exceptions;
using System;
using System.Dynamic;
using System.IO;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Startup
{
    public static class AppSettingsInitializer
    {
        public static void Initialize()
        {
            UpgradeToYml();
            var file = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.yml");

            IConfiguration config = null;

            try
            {
                Console.WriteLine("[x] Read AppSettings files");

                config = new ConfigurationBuilder()
                    .AddYamlFile(file, optional: false, reloadOnChange: true)
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

        public static void TestDatabaseConnection()
        {
            AppSettings.TestConnectionString();
        }

        public static void TestDatabasePermission()
        {
            AppSettings.TestDatabasePermission();
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