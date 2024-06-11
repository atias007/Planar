namespace Planar.API.Common.Entities
{
    public class SetJobPathRequest : IJobFileRequest
    {
        public required string JobFilePath { get; set; }
    }
}