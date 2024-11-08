using NetEscapades.Configuration.Yaml;
using Newtonsoft.Json.Linq;
using Planar.Service.API.Helpers;
using Planar.Service.General;
using Quartz;
using System;
using System.Data.SqlTypes;
using System.Dynamic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Planar.Service.Validation;

public static class ValidationUtil
{
    public static bool IsValidEmail(string? value)
    {
        if (value == null) { return true; }
        return Consts.EmailRegex.IsMatch(value);
    }

    public static bool IsValidCronExpression(string? expression)
    {
        if (expression == null) { return true; }
        if (string.IsNullOrWhiteSpace(expression)) { return false; }
        try
        {
            CronExpression.ValidateExpression(expression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsOnlyDigits(string value)
    {
        if (value == null) { return true; }
        const string pattern = "^[0-9]+$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        return regex.IsMatch(value);
    }

    public static bool IsJobId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) { return false; }
        const string pattern = "^[a-z0-9]{11}$";
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

        var badChars = Path.GetInvalidPathChars();

        // check if value contains any of chars in badChars
        return !Array.Exists(badChars, value.Contains);
    }

    #region SqlDateTime

    public static bool IsValidSqlDateTime(DateTime value)
    {
        var result = value >= SqlDateTime.MinValue.Value && value <= SqlDateTime.MaxValue.Value;
        return result;
    }

    public static bool IsValidSqlDateTime(DateTime? value)
    {
        if (value == null) { return true; }

        var result = value >= SqlDateTime.MinValue.Value && value <= SqlDateTime.MaxValue.Value;
        return result;
    }

    #endregion SqlDateTime

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