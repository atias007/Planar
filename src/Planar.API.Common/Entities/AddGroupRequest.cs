namespace Planar.API.Common.Entities
{
    public class AddGroupRequest
    {
        public string? Name { get; set; }
        public string? Reference1 { get; set; }
        public string? Reference2 { get; set; }
        public string? Reference3 { get; set; }
        public string? Reference4 { get; set; }
        public string? Reference5 { get; set; }
        public Roles RoleId { get; set; }
    }
}