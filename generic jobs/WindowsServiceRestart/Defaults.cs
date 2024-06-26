using Common;
using Microsoft.Extensions.Configuration;

namespace WindowsServiceRestart;

internal class Defaults : BaseDefault, IService
{
    public Defaults()
    {
        RetryCount = 3;
        RetryInterval = TimeSpan.FromSeconds(10);
        MaximumFailsInRow = 5;
    }

    public Defaults(IConfigurationSection section) : base(section)
    {
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}