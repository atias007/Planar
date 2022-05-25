using NetEscapades.Configuration.Yaml;
using Newtonsoft.Json;
using NUnit.Framework;
using Planar.Service.Model.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
            var yaml = File.ReadAllText(@"C:\Users\tsahi_a\source\repos\Planar\Jobs\RunPlanarJob\JobFile.yml");

            var p = new YamlConfigurationFileParser();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            using (var stream = new MemoryStream(byteArray))
            {
                var dict = p.Parse(stream);
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            //yml contains a string containing your YAML
            _ = deserializer.Deserialize<JobMetadata>(yaml);
        }
    }
}