namespace Planar.API.Common.Entities
{
    public class UpdateJobPathRequest : SetJobPathRequest
    {
        public UpdateJobOptions UpdateJobOptions { get; set; } = new UpdateJobOptions();
    }
}