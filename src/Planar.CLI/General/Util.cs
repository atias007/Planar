﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.API.Common.Entities;
using RestSharp;

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
    }
}