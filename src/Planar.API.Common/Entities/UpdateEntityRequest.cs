namespace Planar.API.Common.Entities
{
    public class UpdateEntityRequest
    {
        public int Id { get; set; }

        public string PropertyName { get; set; }

        public string PropertyValue { get; set; }
    }
}