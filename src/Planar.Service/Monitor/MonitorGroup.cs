using Planar.Service.Model;

namespace Planar.Service.Monitor
{
    public class MonitorGroup
    {
        public MonitorGroup(Group group)
        {
            Id = group.Id;
            Name = group.Name;
            AdditionalField1 = group.AdditionalField1;
            AdditionalField2 = group.AdditionalField2;
            AdditionalField3 = group.AdditionalField3;
            AdditionalField4 = group.AdditionalField4;
            AdditionalField5 = group.AdditionalField5;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string? AdditionalField1 { get; set; }
        public string? AdditionalField2 { get; set; }
        public string? AdditionalField3 { get; set; }
        public string? AdditionalField4 { get; set; }
        public string? AdditionalField5 { get; set; }
    }
}