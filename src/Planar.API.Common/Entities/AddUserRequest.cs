using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class AddUserRequest
{
    [YamlMember(Alias = "username")]
    public string? Username { get; set; }

    [YamlMember(Alias = "first name")]
    public string? FirstName { get; set; }

    [YamlMember(Alias = "last name")]
    public string? LastName { get; set; }

    [YamlMember(Alias = "email address 1")]
    public string? EmailAddress1 { get; set; }

    [YamlMember(Alias = "email address 2")]
    public string? EmailAddress2 { get; set; }

    [YamlMember(Alias = "email address 3")]
    public string? EmailAddress3 { get; set; }

    [YamlMember(Alias = "phone number 1")]
    public string? PhoneNumber1 { get; set; }

    [YamlMember(Alias = "phone number 2")]
    public string? PhoneNumber2 { get; set; }

    [YamlMember(Alias = "phone number 3")]
    public string? PhoneNumber3 { get; set; }

    [YamlMember(Alias = "additional field 1")]
    public string? AdditionalField1 { get; set; }

    [YamlMember(Alias = "additional field 2")]
    public string? AdditionalField2 { get; set; }

    [YamlMember(Alias = "additional field 3")]
    public string? AdditionalField3 { get; set; }

    [YamlMember(Alias = "additional field 4")]
    public string? AdditionalField4 { get; set; }

    [YamlMember(Alias = "additional field 5")]
    public string? AdditionalField5 { get; set; }
}