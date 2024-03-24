using System.Xml.Linq;

namespace Planar.Hook.Internals
{
    internal static class Utils
    {
        public static string CleanText(string text)
        {
            var result = text
                .Replace("\r\n", Consts.HookNewLineLogText)
                .Replace("\r", Consts.HookNewLineLogText)
                .Replace("\n", Consts.HookNewLineLogText);

            return new XElement("t", result).LastNode.ToString();
        }
    }
}