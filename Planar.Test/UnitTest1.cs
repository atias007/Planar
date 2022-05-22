using Newtonsoft.Json;
using NUnit.Framework;
using Planar.Service.Model.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
            var yaml = File.ReadAllText(@"R:\CustomsCloud\WorkerJobs\Planar\Planar.Test\JobFile.yml");

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            //yml contains a string containing your YAML
            _ = deserializer.Deserialize<JobMetadata>(yaml);
        }
    }
}