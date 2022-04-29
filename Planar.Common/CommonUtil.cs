using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
            try
            {
                // JobSettings.yml
                var settingsFile = path == null ? SettingsFilename : Path.Combine(path, SettingsFilename);
                var final = ReadSettingsFile(settingsFile);

                // JobSettings.{environment}.yml
                var filename = EnvironmntSettingsFilename.Replace(EnvironmentPlaceholder, Global.Environment);
                var envSettingsFile = path == null ? filename : Path.Combine(path, filename);
                var envSettings = ReadSettingsFile(envSettingsFile);

                // Merge
                final = final.Merge(envSettings);

                return final;
            }
            catch (Exception ex)
            {
                Global.GetLogger<object>().LogError($"Fail to load job settings at Planar.Common.Utils.LoadJobSettings", ex);
                throw;
            }
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