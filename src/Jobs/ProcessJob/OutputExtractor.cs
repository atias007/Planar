using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ProcessJob;

internal class OutputExtractor(string output, ILogger logger)
{
    private static readonly Regex _effectedRowsRegex = new(@"<<planar\.effectedrows:[0-9]+>>", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
    private static readonly Regex _intRegex = new(@"[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

    public int? GetEffectedRows()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(output)) { return null; }
            var matches = _effectedRowsRegex.Matches(output);
            var match = matches.LastOrDefault();
            if (match == null) { return null; }
            if (match.Groups.Count == 0) { return null; }
            var pattern = match.Groups[0].Value;

            matches = _intRegex.Matches(pattern);
            match = matches.LastOrDefault();
            if (match == null) { return null; }
            if (match.Groups.Count == 0) { return null; }
            var value = match.Groups[0].Value;

            if (int.TryParse(value, out var rows))
            {
                return rows;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to read effected rows from process job output");
            return null;
        }
    }
}