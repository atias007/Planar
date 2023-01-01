﻿using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using System;
using System.Text.RegularExpressions;

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
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
            return regex.IsMatch(value);
        }

        public static bool IsJobIdExists(string value, JobKeyHelper jobKeyHelper)
        {
            if (value == null) { return true; }
            var key = jobKeyHelper.GetJobKeyById(value).Result;
            return key != null;
        }

        public static bool IsJobAndGroupExists(string group, string name, JobKeyHelper jobKeyHelper)
        {
            if (string.IsNullOrEmpty(group) && string.IsNullOrEmpty(name)) { return true; }
            if (string.IsNullOrEmpty(group)) { return false; }
            if (string.IsNullOrEmpty(name))
            {
                var result = jobKeyHelper.IsJobGroupExists(group).Result;
                return result;
            }
            else
            {
                var key = jobKeyHelper.GetJobKey($"{group}.{name}").Result;
                return key != null;
            }
        }

        public static bool IsHookExists(string hook)
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
    }
}