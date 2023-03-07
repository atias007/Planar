namespace Planar.API.Common.Entities
{
    public class SetJobRequest<TProperties> : SetJobRequest
        where TProperties : class, new()
    {
        public TProperties? Properties { get; set; }
    }
}