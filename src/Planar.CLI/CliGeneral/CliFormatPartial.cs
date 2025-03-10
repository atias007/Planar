﻿using System;
using System.Text.RegularExpressions;

namespace Planar.CLI.CliGeneral
{
    internal static partial class CliFormat
    {
        private const string logRegex = @"^\[[0-2][0-9]:[0-5][0-9]:[0-5][0-9]\s(INF|WRN|ERR|DBG|TRC|CRT|NON)\]";
        private static readonly Regex _regex = new Regex(logRegex, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

#if NETSTANDARD2_0

        public static string GetLogLineColor(string line)
#else
        public static string? GetLogLineColor(string? line)
#endif
        {
            if (string.IsNullOrWhiteSpace(line)) { return null; }

            var matches = _regex.Matches(line);
            if (matches.Count > 0)
            {
                var level = matches[0].Groups[1].Value;
                var color = GetColor(level);
                return color;
            }

            return null;
        }

        private static string GetColor(string level)
        {
#if NETSTANDARD2_0
            string lowerLevel = level.ToLower(); // Convert to lowercase once

            switch (lowerLevel)
            {
                case "inf":
                    return "white";

                case "wrn":
                    return "wheat1";

                case "err":
                    return "red";

                case "dbg":
                    return "deepskyblue1";

                case "trc":
                    return "lightsalmon1";

                case "crt":
                    return "magenta1";

                case "non":
                    return "white";

                default: // Equivalent to the discard '_' case
                    return "white";
            }
#else
            return level.ToLower() switch
            {
                "inf" => "white",
                "wrn" => "wheat1",
                "err" => "red",
                "dbg" => "deepskyblue1",
                "trc" => "lightsalmon1",
                "crt" => "magenta1",
                "non" => "white",
                _ => "white",
            };
#endif
        }
    }
}