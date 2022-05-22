using NUnit.Framework;
using Planar.Job.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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