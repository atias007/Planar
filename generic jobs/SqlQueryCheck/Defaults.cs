using Common;
using Microsoft.Extensions.Configuration;

namespace SqlQueryCheck;

internal class Defaults : BaseDefault
{
    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    public Defaults()
    {
        RetryCount = 3;
        RetryInterval = TimeSpan.FromSeconds(5);
        MaximumFailsInRow = 5;
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}