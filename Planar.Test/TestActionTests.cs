using NUnit.Framework;
using Planar.Job.Test;
using TestAction;

namespace Planar.Test
{
    public class TestActionTests : BaseJobTest
    {
        [Test]
        public void TestAction1()
        {
            ExecuteJob<ActionJob>();
        }
    }
}