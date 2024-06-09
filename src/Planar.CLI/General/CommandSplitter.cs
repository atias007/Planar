using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.CLI.General;

internal static class CommandSplitter
{
    public static IEnumerable<string> SplitCommandLine(string? commandLine)
    {
        if (string.IsNullOrEmpty(commandLine))
        {
            return new List<string>();
        }

        bool inQuotes = false;

        var split = commandLine.Split(c =>
        {
            if (c == '\"' || c == '\'') { inQuotes = !inQuotes; }
            return !inQuotes && c == ' ';
        });

        var final = split
            .Where(arg => !string.IsNullOrEmpty(arg))
            .Select(arg =>
            {
                return
                arg == null ?
                string.Empty :
                arg
                    .Trim()
                    .TrimMatchingQuotes('\"')
                    .TrimMatchingQuotes('\'');
            });

        return final;
    }

    public static IEnumerable<string?> Split(this string str, Func<char, bool> controller)
    {
        if (str == null)
        {
            yield return null;
        }

        int nextPiece = 0;

        for (int c = 0; c < str?.Length; c++)
        {
            if (controller(str[c]))
            {
                yield return str[nextPiece..c];
                nextPiece = c + 1;
            }
        }

        yield return str?[nextPiece..];
    }

    public static string TrimMatchingQuotes(this string input, char quote)
    {
        if ((input.Length >= 2) &&
            (input[0] == quote) && (input[^1] == quote))
            return input[1..^1];

        return input;
    }
}