using NetEscapades.Configuration.Yaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Planar.Common
{
    public static class JobSettingsLoader
    {
        private const string SettingsFilename = "JobSettings.yml";
        private const string EnvironmentPlaceholder = "{environment}";
        private static readonly string EnvironmntSettingsFilename = $"JobSettings.{EnvironmentPlaceholder}.yml";

        public static IDictionary<string, string?> LoadJobSettings(string jobPath)
        {
            // Load job global config
            var final = new Dictionary<string, string?>(Global.GlobalConfig);

            // Merge settings yml file
            if (string.IsNullOrEmpty(jobPath)) { return final; }
            var fullpath = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, jobPath);
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

        private static Dictionary<string, string?> ReadSettingsFile(string filename)
        {
            var dict = new Dictionary<string, string?>();

            try
            {
                if (!File.Exists(filename)) { return dict; }
                var yml = File.ReadAllText(filename);
                if (string.IsNullOrWhiteSpace(yml)) { return dict; }

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                writer.Write(yml.Trim());
                writer.Flush();
                stream.Position = 0;
                var parser = new YamlConfigurationFileParser();
                var items = parser.Parse(stream);
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