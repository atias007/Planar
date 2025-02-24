using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceCheck;

internal class Defaults : BaseDefault
{
    public Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(1);
    }

    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}