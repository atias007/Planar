namespace Planar.API.Common.Entities
{
    public class UpdateJobRequest : JobOrTriggerKey
    {
        public UpdateJobOptions Options { get; set; } = new();
    }
}