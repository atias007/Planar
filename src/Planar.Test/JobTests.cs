using NUnit.Framework;
using Planar.Service.General.Password;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Planar.Test
{
    public class Tests
    {
        [Test]
        public void TestGetAll()
        {
            const string pattern = "^<hook\\.log\\.(trace|debug|information|warning|error|critical)>.+<\\/hook\\.log\\.(trace|debug|information|warning|error|critical)>$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            var matches = regex.Matches("<hook.log.warning>hiii</hook.log.warning>");
            if (
                matches.Any() &&
                matches[0].Success &&
                matches[0].Groups.Count == 3 &&
                matches[0].Groups[1].Value == matches[0].Groups[2].Value)
            {
                var doc = XDocument.Parse(matches[0].Groups[0].Value);
                var message = doc.Root.Value;
                var level = matches[0].Groups[1].Value;
            }
        }
    }
}