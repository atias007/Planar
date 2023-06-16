using NetEscapades.Configuration.Yaml;
using Newtonsoft.Json.Linq;
using Planar.Service.API.Helpers;
using Planar.Service.General;
using System;
using System.Dynamic;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Planar.Service.Validation
{
    public static class ValidationUtil
    {
        public static bool IsValidEmail(string value)
        {
            if (value == null) { return true; }
            const string pattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9]{2,8}(?:[a-z0-9-]*[a-z0-9])?)\Z";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
            return regex.IsMatch(value);
        }

        public static bool IsOnlyDigits(string value)
        {
            if (value == null) { return true; }
            const string pattern = "^[0-9]+$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            return regex.IsMatch(value);
        }

        public static bool IsJobIdExists(string value, JobKeyHelper jobKeyHelper)
        {
            if (value == null) { return true; }
            var key = jobKeyHelper.GetJobKeyById(value).Result;
            return key != null;
        }

        public static bool IsHookExists(string? hook)
        {
            if (hook == null) { return true; }
            var exists = ServiceUtil.MonitorHooks.ContainsKey(hook);
            return exists;
        }

        public static bool IsPath(string value)
        {
            if (value == null) { return true; }

            const string pattern = @"^(?:\w+\\?)*$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
            return regex.IsMatch(value);
        }

        public static bool IsJsonValid(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) { return false; }
            json = json.Trim();
            if ((json.StartsWith('{') && json.EndsWith('}')) || //For object
                (json.StartsWith('[') && json.EndsWith(']'))) //For array
            {
                try
                {
                    _ = JToken.Parse(json);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static bool IsYmlValid(string? yml)
        {
            if (string.IsNullOrWhiteSpace(yml)) { return false; }
            if (IsJsonValid(yml)) { return false; }

            try
            {
                _ = new DeserializerBuilder().Build().Deserialize<ExpandoObject>(yml);
                new YamlConfigurationFileParser().Parse(yml);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}