using Common;
using Microsoft.Extensions.Configuration;

namespace FolderCheck;

internal class Defaults : BaseDefault
{
    public Defaults(IConfigurationSection section) : base(section, Empty)
    {
    }

    private Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(10);
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}