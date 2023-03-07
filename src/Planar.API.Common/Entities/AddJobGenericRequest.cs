namespace Planar.API.Common.Entities
{
    public class AddJobRequest<TProperties>
        where TProperties : class, new()
    {
        public TProperties? Properties { get; set; }
    }
}