using Common;
using Microsoft.Extensions.Configuration;

namespace IISRecycle;

internal class ApplicationPool : BaseOperation, INamedCheckElement, IVetoEntity
{
    public ApplicationPool(IConfigurationSection section, BaseDefault @default) : base(section, @default)
    {
        Name = section.GetValue<string>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name") ?? string.Empty;
        ServerConfigFile = section.GetValue<string>("server config file") ?? string.Empty;
    }

    public ApplicationPool(ApplicationPool pool) : base(pool)
    {
        Name = pool.Name;
        HostGroupName = pool.HostGroupName;
        ServerConfigFile = pool.ServerConfigFile;
    }

    public string Name { get; set; }
    public string HostGroupName { get; private set; }
    public string ServerConfigFile { get; private set; }

    //// --------------------------------------- ////

    public string Key => Name;

    // internal use for relative urls
    public string? Host { get; set; }
}