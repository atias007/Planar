namespace Planar.Monitor.Hook
{
    public interface IMonitorGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? AdditionalField1 { get; set; }
        public string? AdditionalField2 { get; set; }
        public string? AdditionalField3 { get; set; }
        public string? AdditionalField4 { get; set; }
        public string? AdditionalField5 { get; set; }
    }
}