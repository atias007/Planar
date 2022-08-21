using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planar.Common
{
    public static class JobSettingsLoader
    {
        private const string SettingsFilename = "JobSettings.yml";
        private const string EnvironmentPlaceholder = "{environment}";
        private static readonly string EnvironmntSettingsFilename = $"JobSettings.{EnvironmentPlaceholder}.yml";

        public static Dictionary<string, string> LoadJobSettings(string jobPath)
        {
            // Load job global parameters
            var final = Global.Parameters;

            // Merge settings yml file
            if (string.IsNullOrEmpty(jobPath)) { return new Dictionary<string, string>(); }
            var fullpath = Path.Combine(FolderConsts.BasePath, FolderConsts.Data, FolderConsts.Jobs, jobPath);
            var location = new DirectoryInfo(fullpath);
            if (!location.Exists) { return final; }

            var jobSettings = LoadJobSettingsFiles(location.FullName);
            final = final.Merge(jobSettings);
            return final;
        }

        private static Dictionary<string, string> LoadJobSettingsFiles(string path)
        {
            var result = new Dictionary<string, string>();

            // JobSettings.yml
            var files = GetFiles(path, SettingsFilename);

            foreach (var f in files)
            {
                var temp = ReadSettingsFile(f);
                result = result.Merge(temp);
            }

            // JobSettings.{environment}.yml
            var filename = EnvironmntSettingsFilename.Replace(EnvironmentPlaceholder, Global.Environment);
            files = GetFiles(path, filename);

            foreach (var f in files)
            {
                var temp = ReadSettingsFile(f);
                result = result.Merge(temp);
            }

            return result;
        }

        private static IEnumerable<string> GetFiles(string path, string filename)
        {
            var files = Directory
                .GetFiles(path, filename, SearchOption.AllDirectories)
                .OrderByDescending(f => f);

            return files;
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
                throw new ArgumentNullException($"Error while reading settings file {SettingsFilename}", ex);
            }

            return dict;
        }
    }
}