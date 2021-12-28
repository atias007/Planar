using Newtonsoft.Json;
using NUnit.Framework;
using Planner.Service.Model.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planner.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestSerialize()
        {
            var plan = new JobMetadata
            {
                JobType = "RunPlannerJob",
                Name = "some job name",
                Group = "null or group name",
                Description = "some job description\r\nwith line breaks being preserved",
                Durable = true,
                GlobalParameters = new()
                {
                    { "Param1", "Value1" },
                    { "Param2", "Value2" },
                    { "Param3", "Value3" },
                },
                JobData = new()
                {
                    { "key 1", "value 1" },
                    { "key 2", "value 2" },
                    { "key 3", "value 3" }
                },
                Properties = new()
                {
                    { "JobPath", @"r:\cloud" },
                    { "FileName", "assembly.dll" },
                    { "TypeName", "MyClass" },
                },
                SimpleTriggers = new List<JobSimpleTriggerMetadata>
                {
                    new JobSimpleTriggerMetadata
                    {
                        Start = DateTime.Now.Add(new TimeSpan(0, 0, 0)),
                        End = DateTime.Now.Add(new TimeSpan(23, 59, 0)),
                        Interval = TimeSpan.FromMinutes(1),
                        Priority = 1,
                        RepeatCount = 10,
                        MisfireBehaviour = 0,
                        Calendar = "HebrewCalendar",
                        RetrySpan = TimeSpan.FromHours(1),
                        TriggerData = new()
                        {
                            { "key 4", "value 4" },
                            { "key 5", "value 5" },
                            {"key 6", "value 6" }
                        }
                    }
                },
                CronTriggers = new List<JobCronTriggerMetadata>
                {
                    new JobCronTriggerMetadata
                    {
                        CronExpression = "0 15 10 ? * MON-FRI",
                        MisfireBehaviour = 0,
                        Calendar = "HebrewCalendar",
                        RetrySpan = TimeSpan.FromHours(1),
                        TriggerData = new()
                        {
                            { "key 7",  "value 7" },
                            { "key 8",  "value 8" },
                            { "key 9",  "value 9" }
                        },
                        Priority = 1,
                    }
                }
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(plan);
            Console.WriteLine(yaml);
            Assert.Pass();
        }

        [Test]
        public void TestDeserialize()
        {
            var yaml = File.ReadAllText(@"R:\CustomsCloud\WorkerJobs\Planner\Planner.Test\JobFile.yml");

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            //yml contains a string containing your YAML
            var plan = deserializer.Deserialize<JobMetadata>(yaml);
        }

        [Test]
        public void TestSerialize2()
        {
            var dic = new Dictionary<string, string>
            {
                {"Key1", "Value1" },
                {"Key2", "Value2" },
                {"Key3", "Value3" },
            };

            var serializer = new SerializerBuilder().Build();
            var yml = serializer.Serialize(dic);
        }

        [Test]
        public void TestSerializeException()
        {
            int a = 1, b = 0;
            try
            {
                var c = a / b;
            }
            catch (Exception ex)
            {
                var myEx = new MyException("fail....", ex);
                var json = JsonConvert.SerializeObject(myEx);
                var myEx2 = JsonConvert.DeserializeObject<MyException>(json);

                using (Stream s = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(s, myEx);
                    s.Position = 0; // Reset stream position
                    var exx = (MyException)formatter.Deserialize(s);
                }

                throw;
            }
        }
    }
}