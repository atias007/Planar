using System.Xml.Linq;

namespace Planar.Hook.Internals
{
    internal static class Utils
    {
        public static string CleanText(string text)
        {
            var result = text
                .Replace("\r\n", Consts.HookNewLineLogText1)
                .Replace("\r", Consts.HookNewLineLogText1)
                .Replace("\n", Consts.HookNewLineLogText1);

            return new XElement("t", result).LastNode.ToString();
        }
    }
}