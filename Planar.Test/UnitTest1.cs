using NetEscapades.Configuration.Yaml;
using NUnit.Framework;
using Planar.Service.Model.Metadata;
using Planar.TeamsMonitorHook;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestDeserialize()
        {
            var hook = new TeamHook();
            hook.Handle(null).Wait();
        }
    }
}