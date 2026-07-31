using NUnit.Framework;
using Planar.Service.General;

namespace Planar.Test;

public class Tests
{
    [Test]
    public void ServiceUtilIsFileExists()
    {
        var result = ServiceUtil.IsFileExists(@"..\..\CommonJob.dll");
        Assert.AreEqual(true, result);
    }
}