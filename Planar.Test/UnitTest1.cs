using NUnit.Framework;
using Planar.TeamsMonitorHook;
using System;

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
            var timespan = TimeSpan.FromHours(24);
            var text1 = $"{timespan:\\(d\\)\\ hh\\:mm\\:ss}";
            timespan = TimeSpan.FromHours(22).Add(TimeSpan.FromMinutes(33));
            var text2 = timespan.ToString("c");

            var hook = new TeamHook();
            hook.Handle(null).Wait();
            Assert.Pass();
        }
    }
}