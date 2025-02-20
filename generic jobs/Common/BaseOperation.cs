using Microsoft.Extensions.Configuration;

namespace Common;

public abstract class BaseOperation : BaseDefault
{
    protected BaseOperation(IConfigurationSection section, BaseDefault @default) : base(section, @default)
    {
    }

    protected BaseOperation(BaseOperation operation) : base(operation)
    {
    }

    public new OperationStatus RunStatus { get; set; }
}