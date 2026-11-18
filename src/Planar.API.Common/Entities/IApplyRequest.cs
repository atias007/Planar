namespace Planar.API.Common.Entities;

public interface IApplyRequest
{
    string Source { get; }
    string Version { get; }
    string Key { get; }
}

////// ===== APPLY REQUEST PROPERTIES ===== ////

////[YamlMember(Alias = "source")]
////public string Source { get; set; } = string.Empty;

////[YamlMember(Alias = "kind")]
////public string Kind { get; set; } = string.Empty;

////[YamlMember(Alias = "version")]
////public string Version { get; set; } = string.Empty;

////// ==================================== ////