namespace Planar.API.Common.Entities
{
    public class UpdateEntityRequestByName : UpdateEntityRequest
    {
        public string Name { get; set; } = null!;
    }
}