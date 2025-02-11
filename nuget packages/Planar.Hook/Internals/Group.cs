namespace Planar.Hook
{
    internal class Group : IMonitorGroup
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

#if NETSTANDARD2_0
        public string AdditionalField1 { get; set; }
        public string AdditionalField2 { get; set; }
        public string AdditionalField3 { get; set; }
        public string AdditionalField4 { get; set; }
        public string AdditionalField5 { get; set; }
#else
        public string? AdditionalField1 { get; set; }

        public string? AdditionalField2 { get; set; }

        public string? AdditionalField3 { get; set; }

        public string? AdditionalField4 { get; set; }

        public string? AdditionalField5 { get; set; }
#endif
    }
}