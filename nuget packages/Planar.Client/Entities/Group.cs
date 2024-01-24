namespace Planar.Client.Entities
{
    public class Group
    {
        public string Name { get; set; } = null!;
        public string? AdditionalField1 { get; set; }
        public string? AdditionalField2 { get; set; }
        public string? AdditionalField3 { get; set; }
        public string? AdditionalField4 { get; set; }
        public string? AdditionalField5 { get; set; }
        public Roles Role { get; set; }
    }
}