namespace Planar.API.Common.Entities
{
    public class LovItem
    {
        public LovItem()
        {
        }

        public LovItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; }
    }
}