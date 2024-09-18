using Microsoft.Extensions.Configuration;

namespace FolderCheck;

internal class Host(IConfigurationSection section)
{
    public string GroupName { get; private set; } = section.GetValue<string>("group name") ?? string.Empty;
    public IEnumerable<string> Hosts { get; private set; } = section.GetRequiredSection("hosts").Get<string[]>()?.ToList() ?? [];
}