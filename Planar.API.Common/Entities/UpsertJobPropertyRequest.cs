namespace Planar.API.Common.Entities
{
    public class UpsertJobPropertyRequest : JobOrTriggerKey
    {
        public string PropertyKey { get; set; }

        public string PropertyValue { get; set; }
    }
}