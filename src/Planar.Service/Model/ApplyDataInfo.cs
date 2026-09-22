using Quartz;

namespace Planar.Service.Model;

internal record ApplyDataInfo
{
    public TriggerKey? TriggerKey { get; init; }
    public JobKey? JobKey { get; init; }
    public required string Description { get; init; }
    public required object AdditionalInfo { get; init; }
}