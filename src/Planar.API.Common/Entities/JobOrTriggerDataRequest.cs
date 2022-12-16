namespace Planar.API.Common.Entities
{
    public class JobOrTriggerDataRequest : JobOrTriggerKey
    {
        public string DataKey { get; set; }

        public string DataValue { get; set; }
    }
}