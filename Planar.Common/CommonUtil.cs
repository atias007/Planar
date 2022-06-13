using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planar.Common
{
    public static class CommonUtil
    {
        private const string SettingsFilename = "JobSettings.yml";
        private const string EnvironmentPlaceholder = "{environment}";
        private static readonly string EnvironmntSettingsFilename = $"JobSettings.{EnvironmentPlaceholder}.yml";

        public static Dictionary<string, string> LoadJobSettings(string path = null)
        {
            var result = new Dictionary<string, string>();
            var relativePath = path ?? ".";

            // JobSettings.yml
            var settingsFile = path == null ? SettingsFilename : Path.Combine(path, SettingsFilename);
            var files = Directory.GetFiles(relativePath, SettingsFilename, SearchOption.AllDirectories)
                .ToList()
                .OrderByDescending(f => f);

            foreach (var f in files)
            {
                var temp = ReadSettingsFile(f);
                result = result.Merge(temp);
            }

            // JobSettings.{environment}.yml
            var filename = EnvironmntSettingsFilename.Replace(EnvironmentPlaceholder, Global.Environment);
            var envSettingsFile = path == null ? filename : Path.Combine(path, filename);
            files = Directory.GetFiles(relativePath, filename, SearchOption.AllDirectories)
                .ToList()
                .OrderByDescending(f => f);

            foreach (var f in files)
            {
                var temp = ReadSettingsFile(f);
                result = result.Merge(temp);
            }

            return result;
        }

        private static Dictionary<string, string> ReadSettingsFile(string filename)
        {
            var dict = new Dictionary<string, string>();

            try
            {
                if (File.Exists(filename))
                {
                    var yml = File.ReadAllText(filename);
                    var serializer = new DeserializerBuilder().Build();
                    dict = serializer.Deserialize<Dictionary<string, string>>(yml);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error while reading settings file {SettingsFilename}", ex);
            }

            return dict;
        }
    }
}