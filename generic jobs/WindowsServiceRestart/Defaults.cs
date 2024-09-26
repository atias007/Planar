using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceRestart;

internal class Defaults : BaseDefault, IService
{
    public Defaults()
    {
        RetryCount = 3;
        RetryInterval = TimeSpan.FromSeconds(10);
    }

    public Defaults(Defaults defaults) : base(defaults)
    {
        RetryCount = defaults.RetryCount;
        RetryInterval = defaults.RetryInterval;
    }

    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}