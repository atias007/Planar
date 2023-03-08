namespace Planar.Monitor.Hook
{
    public class Group : IMonitorGroup
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Reference1 { get; set; }

        public string? Reference2 { get; set; }

        public string? Reference3 { get; set; }

        public string? Reference4 { get; set; }

        public string? Reference5 { get; set; }
    }
}