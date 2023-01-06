using System.Text.RegularExpressions;

namespace Planar.Service.General
{
    internal static class Extensions
    {
        public static string SplitWords(this string value)
        {
            const string spacer = " ";
            const string template = @"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])";
            var r = new Regex(template);
            var result = r.Replace(value, spacer);
            return result;
        }
    }
}