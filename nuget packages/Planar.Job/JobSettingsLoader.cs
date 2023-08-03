using NetEscapades.Configuration.Yaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Planar.Job
{
    internal static class JobSettingsLoader
    {
        private const string SettingsFilename = "JobSettings.yml";
        private const string EnvironmentPlaceholder = "{environment}";
        private static readonly string EnvironmntSettingsFilename = $"JobSettings.{EnvironmentPlaceholder}.yml";

        public static IDictionary<string, string?> LoadJobSettings(Dictionary<string, string?> globalSettings)
        {
            // Load job global config
            var final = new Dictionary<string, string?>(globalSettings);

            // Merge settings yml file
            var fullpath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var location = new DirectoryInfo(fullpath);
            if (!location.Exists) { return final; }

            var jobSettings = LoadJobSettingsFiles(location.FullName);
            final = final.Merge(jobSettings);

            var result = new SortedDictionary<string, string?>(final);
            return result;
        }

        private static Dictionary<string, string?> LoadJobSettingsFiles(string path)
        {
            var result = new Dictionary<string, string?>();

            // JobSettings.yml
            var files = GetFiles(path, SettingsFilename);

            foreach (var f in files)
            {
                var temp = ReadSettingsFile(f);
                result = result.Merge(temp);
            }

            // JobSettings.{environment}.yml
            var filename = EnvironmntSettingsFilename.Replace(EnvironmentPlaceholder, "Development");
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

        private static Dictionary<string, string?> ReadSettingsFile(string filename)
        {
            var dict = new Dictionary<string, string?>();

            try
            {
                if (!File.Exists(filename)) { return dict; }
                var yml = File.ReadAllText(filename);
                var parser = new YamlConfigurationFileParser();
                var items = parser.Parse(yml.Trim());
                dict = new Dictionary<string, string?>(items);
                return dict;
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException($"error while reading settings file {filename}", ex);
            }
        }
    }
}