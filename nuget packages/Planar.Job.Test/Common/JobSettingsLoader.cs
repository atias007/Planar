using NetEscapades.Configuration.Yaml;
using System;
using System.Collections.Generic;
using System.IO;

namespace Planar.Job.Test
{
    internal static class JobSettingsLoader
    {
        private const string SettingsFilename1 = "JobSettings.yml";
        private const string SettingsFilename2 = "JobSettings.UnitTest.yml";

        public static Dictionary<string, string> LoadJobSettingsForUnitTest(Type type)
        {
            var directory = new FileInfo(type.Assembly.Location).Directory;

            // Load job global config
            var jobSettings = new Dictionary<string, string>();

            // Merge settings yml file
            if (directory == null || !directory.Exists) { return jobSettings; }

            jobSettings = LoadJobSettingsFiles(directory.FullName);
            return jobSettings;
        }

        private static Dictionary<string, string> LoadJobSettingsFiles(string path)
        {
            var result = new Dictionary<string, string>();

            // JobSettings.yml
            var file = Path.Combine(path, SettingsFilename1);
            var temp = ReadSettingsFile(file);
            result = result.Merge(temp);

            // JobSettings.UnitTest.yml
            file = Path.Combine(path, SettingsFilename2);
            temp = ReadSettingsFile(file);
            result = result.Merge(temp);

            return result;
        }

        private static Dictionary<string, string> ReadSettingsFile(string filename)
        {
            var dict = new Dictionary<string, string>();

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
                dict = new Dictionary<string, string>(items);

                return dict;
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException($"error while reading settings file {filename}", ex);
            }
        }
    }
}