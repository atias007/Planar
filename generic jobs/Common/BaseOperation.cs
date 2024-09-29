using Microsoft.Extensions.Configuration;

namespace Common;

public abstract class BaseOperation : BaseActiveElement
{
    protected BaseOperation(IConfigurationSection section) : base(section)
    {
    }

    protected BaseOperation(BaseOperation operation) : base(operation)
    {
    }

    public OperationStatus OperationStatus { get; set; }
}