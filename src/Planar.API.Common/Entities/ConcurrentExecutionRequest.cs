namespace Planar.API.Common.Entities
{
    public class ConcurrentExecutionRequest : MaxConcurrentExecutionRequest
    {
        public string? Server { get; set; }
        public string? InstanceId { get; set; }
    }
}