using Common;
using Microsoft.Extensions.Configuration;

namespace RedisOperations;

internal class Defaults : BaseDefault
{
    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    private Defaults()
    {
        RetryCount = 3;
        RetryInterval = TimeSpan.FromSeconds(30);
        AllowedFailSpan = null;
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}