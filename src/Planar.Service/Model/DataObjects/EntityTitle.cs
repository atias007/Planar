namespace Planar.Service.Model.DataObjects
{
    public class EntityTitle
    {
        public EntityTitle(int id, string firstName)
        {
            Id = id;
            FirstName = firstName;
        }

        public EntityTitle(int id, string firstName, string? lastName) : this(id, firstName)
        {
            LastName = lastName;
        }

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, Name: {FirstName} {LastName}".Trim();
        }
    }
}