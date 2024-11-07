using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.API.Common.Entities;
using RestSharp;
using System.Text.RegularExpressions;
using System;

namespace Planar.CLI.General
{
    internal static class Util
    {
        public static string? LastJobOrTriggerId { get; set; }

        public static string BeautifyJson(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }

            var result = JToken.Parse(json).ToString(Formatting.Indented);
            return result;
        }

        public static void SetLastJobOrTriggerId(RestResponse<PlanarIdResponse> response)
        {
            if (response == null) { return; }
            if (!response.IsSuccessful) { return; }
            if (response.Data == null) { return; }
            if (string.IsNullOrEmpty(response.Data.Id)) { return; }
            LastJobOrTriggerId = response.Data.Id;
        }

        public static bool IsJobId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) { return false; }
            const string pattern = "^[a-z0-9]{11}$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            return regex.IsMatch(value);
        }
    }
}