using NUnit.Framework;
using Planar.Job.Test;
using System.Collections.Generic;
using TestAction;

namespace Planar.Test
{
    public class TestActionTests : BaseJobTest
    {
        [Test]
        public void TestAction1()
        {
            var data = new Dictionary<string, object>
            {
                { "Value", 100.2 }
            };

            ExecuteJob<ActionJob>(data);
        }
    }
}