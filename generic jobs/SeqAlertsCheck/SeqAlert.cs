using Common;
using Microsoft.Extensions.Configuration;

namespace SeqAlertsCheck;

internal class SeqAlert(IConfigurationSection section, Defaults defaults) : BaseDefault(section, defaults), ICheckElement, IVetoEntity
{
    public string Key { get; set; } = string.Empty;
}