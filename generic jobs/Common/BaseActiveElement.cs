using Microsoft.Extensions.Configuration;

namespace Common;

public abstract class BaseActiveElement
{
    protected BaseActiveElement(IConfigurationSection section)
    {
        Active = section.GetValue<bool?>("active") ?? true;
    }

    protected BaseActiveElement(BaseActiveElement source)
    {
        Active = source.Active;
    }

    protected BaseActiveElement()
    {
        Active = true;
    }

    public bool Active { get; }
}