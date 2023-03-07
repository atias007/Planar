namespace Planar.API.Common.Entities
{
    public class JobOrTriggerDataRequest : JobOrTriggerKey
    {
        public string DataKey { get; set; } = string.Empty;

        public string? DataValue { get; set; }
    }
}