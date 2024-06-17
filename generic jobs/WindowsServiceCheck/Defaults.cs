using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceCheck;

internal class Defaults : BaseDefault, IService
{
    public Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(1);
        MaximumFailsInRow = 5;
    }

    public Defaults(IConfigurationSection section) : base(section)
    {
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}