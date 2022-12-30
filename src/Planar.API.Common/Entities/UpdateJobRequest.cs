namespace Planar.API.Common.Entities
{
    public class UpdateJobRequest<T> : SetJobRequest<T>
        where T : class, new()
    {
        public UpdateJobOptions UpdateJobOptions { get; set; }
    }
}