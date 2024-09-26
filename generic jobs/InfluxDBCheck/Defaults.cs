using Common;
using Microsoft.Extensions.Configuration;

namespace InfluxDBCheck;

internal class Defaults : BaseDefault
{
    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    public Defaults()
    {
        RetryCount = 3;
        RetryInterval = TimeSpan.FromSeconds(3);
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}