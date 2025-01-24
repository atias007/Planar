using Common;
using Microsoft.Extensions.Configuration;

namespace SeqAlertsCheck;

internal class Defaults : BaseDefault
{
    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    public Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(1);
        AllowedFailSpan = null;
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}