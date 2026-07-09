using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliUpdateGroupRequest : CliAddGroupRequest
{
    [ActionProperty(ShortName = "f1", LongName = "field1", Name = "additional field 1")]
    public string? AdditionalField1 { get; set; }

    [ActionProperty(ShortName = "f2", LongName = "field2", Name = "additional field 2")]
    public string? AdditionalField2 { get; set; }

    [ActionProperty(ShortName = "f3", LongName = "field3", Name = "additional field 3")]
    public string? AdditionalField3 { get; set; }

    [ActionProperty(ShortName = "f4", LongName = "field4", Name = "additional field 4")]
    public string? AdditionalField4 { get; set; }

    [ActionProperty(ShortName = "f5", LongName = "field5", Name = "additional field 5")]
    public string? AdditionalField5 { get; set; }
}