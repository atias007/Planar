using Microsoft.Extensions.Configuration;

namespace Common;

public class Host(IConfigurationSection section)
{
    public string GroupName { get; private set; } = section.GetValue<string>("group name") ?? string.Empty;
    public IEnumerable<string> Hosts { get; private set; } = section.GetRequiredSection("hosts").Get<string[]>()?.ToList()?.Distinct() ?? [];
}