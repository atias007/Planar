namespace Planar.API.Common.Entities
{
    public class UpdateEntityRequest
    {
        public string PropertyName { get; set; } = string.Empty;

        public string? PropertyValue { get; set; }
    }
}