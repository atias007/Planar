using NetEscapades.Configuration.Yaml;
using System;
using System.Collections.Generic;
using System.IO;
using Planar.Common;

namespace Planar.Job.Test
{
    internal static class JobSettingsLoader
    {
        private const string SettingsFilename1 = "JobSettings.yml";
        private const string SettingsFilename2 = "JobSettings.UnitTest.yml";

#if NETSTANDARD2_0

        public static Dictionary<string, string> LoadJobSettingsForUnitTest(Type type)
#else
        public static Dictionary<string, string?> LoadJobSettingsForUnitTest(Type type)
#endif
        {
            var directory = new FileInfo(type.Assembly.Location).Directory;

#if NETSTANDARD2_0
            // Load job global config
            var jobSettings = new Dictionary<string, string>();
#else
            // Load job global config
            var jobSettings = new Dictionary<string, string?>();
#endif

            // Merge settings yml file
            if (directory == null || !directory.Exists) { return jobSettings; }

            jobSettings = LoadJobSettingsFiles(directory.FullName);
            return jobSettings;
        }

#if NETSTANDARD2_0

        private static Dictionary<string, string> LoadJobSettingsFiles(string path)
        {
            var result = new Dictionary<string, string>();

#else
        private static Dictionary<string, string?> LoadJobSettingsFiles(string path)
        {
            var result = new Dictionary<string, string?>();

#endif

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

#if NETSTANDARD2_0

        private static Dictionary<string, string> ReadSettingsFile(string filename)
        {
            var dict = new Dictionary<string, string>();
#else
        private static Dictionary<string, string?> ReadSettingsFile(string filename)
        {
            var dict = new Dictionary<string, string?>();
#endif

            try
            {
                if (!File.Exists(filename)) { return dict; }
                var yml = File.ReadAllText(filename);
                if (string.IsNullOrWhiteSpace(yml)) { return dict; }

#if NETSTANDARD2_0
                using (var stream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(yml.Trim());
                        writer.Flush();
                        stream.Position = 0;
                        var parser = new YamlConfigurationFileParser();
                        var items = parser.Parse(stream);
                        dict = new Dictionary<string, string>(items);
                    }
                }
#else
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                writer.Write(yml.Trim());
                writer.Flush();
                stream.Position = 0;
                var parser = new YamlConfigurationFileParser();
                var items = parser.Parse(stream);
                dict = new Dictionary<string, string?>(items);
#endif

                return dict;
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException($"error while reading settings file {filename}", ex);
            }
        }
    }
}