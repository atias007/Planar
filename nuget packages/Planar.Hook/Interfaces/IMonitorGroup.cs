using System.Collections.Generic;

namespace Planar.Hook
{
    public interface IMonitorGroup
    {
        string Name { get; }
        IEnumerable<IMonitorUser> Users { get; }

#if NETSTANDARD2_0
        string AdditionalField1 { get; }
        string AdditionalField2 { get; }
        string AdditionalField3 { get; }
        string AdditionalField4 { get; }
        string AdditionalField5 { get; }
#else
        string? AdditionalField1 { get; }
        string? AdditionalField2 { get; }
        string? AdditionalField3 { get; }
        string? AdditionalField4 { get; }
        string? AdditionalField5 { get; }
#endif
    }
}