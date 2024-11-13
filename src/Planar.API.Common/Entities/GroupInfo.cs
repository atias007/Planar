namespace Planar.API.Common.Entities
{
    public class GroupInfo
    {
        public string? Name { get; set; }

        [DisplayFormat(Format = "N0")]
        public int UsersCount { get; set; }

        public string? Role { get; set; }
    }
}