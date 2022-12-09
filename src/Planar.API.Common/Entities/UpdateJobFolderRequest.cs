namespace Planar.API.Common.Entities
{
    public class UpdateJobFolderRequest : AddJobFoldeRequest
    {
        public UpdateJobOptions UpdateJobOptions { get; set; }
    }
}