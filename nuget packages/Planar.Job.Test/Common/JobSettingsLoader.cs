using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planar.Job.Test
{
    internal static class JobSettingsLoader
    {
        private const string SettingsFilename = "JobSettings.yml";
        private const string EnvironmentPlaceholder = "{environment}";
        private static readonly string EnvironmntSettingsFilename = $"JobSettings.{EnvironmentPlaceholder}.yml";

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
            var files = GetFiles(path, SettingsFilename);

            foreach (var f in files)
            {
                var temp = ReadSettingsFile(f);
                result = result.Merge(temp);
            }

            // JobSettings.{environment}.yml
            var filename = EnvironmntSettingsFilename.Replace(EnvironmentPlaceholder, UnitTestConsts.Environment);
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
                if (!File.Exists(filename)) { return dict; }

                var yml = File.ReadAllText(filename);
                if (yml == null) { return dict; }

                yml = yml.Trim();
                if (string.IsNullOrEmpty(yml)) { return dict; }

                var serializer = new DeserializerBuilder().Build();
                dict = serializer.Deserialize<Dictionary<string, string>>(yml);
                return dict;
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException($"error while reading settings file {filename}", ex);
            }
        }
    }
}