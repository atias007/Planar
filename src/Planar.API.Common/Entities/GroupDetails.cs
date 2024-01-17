using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class GroupDetails
    {
        public string Name { get; set; } = string.Empty;
        public string? AdditionalField1 { get; set; }
        public string? AdditionalField2 { get; set; }
        public string? AdditionalField3 { get; set; }
        public string? AdditionalField4 { get; set; }
        public string? AdditionalField5 { get; set; }
        public string? Role { get; set; }

        public List<EntityTitle> Users { get; set; } = new();
    }
}