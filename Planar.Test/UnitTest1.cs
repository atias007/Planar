using NUnit.Framework;
using Planar.TeamsMonitorHook;

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
            Assert.Pass();
        }
    }
}