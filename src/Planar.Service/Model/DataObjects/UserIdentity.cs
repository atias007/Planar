namespace Planar.Service.Model.DataObjects
{
    public class UserIdentity
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public int RoleId { get; set; }
        public byte[] Password { get; set; }
        public byte[] Salt { get; set; }
    }
}