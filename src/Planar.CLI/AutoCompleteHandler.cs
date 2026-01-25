using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.CLI;

internal class AutoCompleteHandler(IEnumerable<CliActionMetadata> actions) : IAutoCompleteHandler
{
    private readonly string[] _modules = actions
        .Select(a => a.Module)
        .Union(
            actions
                .SelectMany(a => a.Commands)
                .GroupBy(g => g)
                .Where(g => g.Count() == 1)
                .Select(g => g.Key)
        )
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public char[] Separators { get; set; } = [' '];

    public string[] GetSuggestions(string text, int index)
    {
        return GetSuggestionsSafe(text, index);
    }

    private string[] GetSuggestionsSafe(string text, int index)
    {
        try
        {
            return GetSuggestionsInner(text, index);
        }
        catch
        {
            // **** DO NOTHING ****
            return [];
        }
    }

    private string[] GetSuggestionsInner(string text, int index)
    {
        if (string.IsNullOrWhiteSpace(text)) { return []; }
        var split = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 0) { return []; }
        if (text.EndsWith(' ')) { split = split.Append(string.Empty).ToArray(); }
        var moduleText = split[0];
        if (split.Length == 1)
        {
            return Array.FindAll(_modules, m => m.StartsWith(text, StringComparison.OrdinalIgnoreCase));
        }

        var modules = actions.Where(a => string.Equals(a.Module, moduleText, StringComparison.OrdinalIgnoreCase));
        if (!modules.Any()) { return []; }

        var commandText = split[1];

        if (split.Length == 2)
        {
            var commands = modules
                .SelectMany(a => a.Commands)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Array.FindAll(commands, c => c.StartsWith(commandText, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var command = modules
                .Where(m => m.Commands.Contains(commandText, StringComparer.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (command == null) { return []; }

            var prm = split.LastOrDefault();
            if (string.IsNullOrWhiteSpace(prm))
            {
                return command.Arguments
                    .Where(a => !string.IsNullOrWhiteSpace(a.LongName))
                    .Select(a => $"--{a.LongName}" ?? string.Empty)
                    .ToArray();
            }
            else
            {
                return command.Arguments
                    .Where(a => ($"--{a.LongName}").StartsWith(prm, StringComparison.OrdinalIgnoreCase))
                    .Select(a => $"--{a.LongName}")
                    .ToArray();
            }
        }
    }
}