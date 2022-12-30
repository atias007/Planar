namespace Planar.API.Common.Entities
{
    public class EntityIdResponse
    {
        public EntityIdResponse(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
    }
}