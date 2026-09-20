namespace Planar.API.Common.Entities;

public interface IApplyRequest
{
    string Kind { get; set; }
    string Source { get; set; }
    string Version { get; set; }
}

//// ===== APPLY REQUEST PROPERTIES ===== ////

////[YamlMember(Alias = "source")]
////public string Source { get; set; } = string.Empty;

////[YamlMember(Alias = "kind")]
////public string Kind { get; set; } = string.Empty;

////[YamlMember(Alias = "version")]
////public string Version { get; set; } = string.Empty;

//// ==================================== ////