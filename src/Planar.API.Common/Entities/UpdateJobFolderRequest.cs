namespace Planar.API.Common.Entities
{
    public class UpdateJobFolderRequest : SetJobFoldeRequest
    {
        public UpdateJobOptions UpdateJobOptions { get; set; }
    }
}