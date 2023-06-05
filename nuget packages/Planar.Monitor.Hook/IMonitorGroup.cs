namespace Planar.Monitor.Hook
{
    public interface IMonitorGroup
    {
        public int Id { get; }
        public string Name { get; }
        public string? AdditionalField1 { get; }
        public string? AdditionalField2 { get; }
        public string? AdditionalField3 { get; }
        public string? AdditionalField4 { get; }
        public string? AdditionalField5 { get; }
    }
}