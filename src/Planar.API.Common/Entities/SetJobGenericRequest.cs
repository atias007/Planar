namespace Planar.API.Common.Entities
{
    public class SetJobRequest<TProperties>
        where TProperties : class, new()
    {
        public TProperties Properties { get; set; }
    }
}