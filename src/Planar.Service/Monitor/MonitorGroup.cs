using Planar.Service.Model;

namespace Planar.Service.Monitor
{
    public class MonitorGroup
    {
        public MonitorGroup(Group group)
        {
            Id = group.Id;
            Name = group.Name;
            Reference1 = group.Reference1;
            Reference2 = group.Reference2;
            Reference3 = group.Reference3;
            Reference4 = group.Reference4;
            Reference5 = group.Reference5;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string? Reference1 { get; set; }
        public string? Reference2 { get; set; }
        public string? Reference3 { get; set; }
        public string? Reference4 { get; set; }
        public string? Reference5 { get; set; }
    }
}