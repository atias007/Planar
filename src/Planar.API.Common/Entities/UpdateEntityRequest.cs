namespace Planar.API.Common.Entities
{
    public class UpdateEntityRequest
    {
        public int Id { get; set; }

        public string PropertyName { get; set; } = string.Empty;

        public string? PropertyValue { get; set; }
    }
}