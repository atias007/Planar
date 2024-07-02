using Planar.API.Common.Entities;

namespace Planar.Service.Model.DataObjects
{
    public class UserIdentity
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public Roles Role { get; set; }

        public string Surename { get; set; } = null!;
        public string? GivenName { get; set; } = null!;

        public byte[] Password { get; set; } = null!;

        public byte[] Salt { get; set; } = null!;

        public string Fullname => $"{Surename} {GivenName}".Trim();
    }
}