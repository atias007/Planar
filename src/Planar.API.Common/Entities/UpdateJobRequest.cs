namespace Planar.API.Common.Entities
{
    public class UpdateJobRequest : IJobFileRequest
    {
        public required string JobFilePath { get; set; }

        public UpdateJobOptions Options { get; set; } = new();
    }
}