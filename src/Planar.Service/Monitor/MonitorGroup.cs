using Planar.Service.Model;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Service.Monitor;

public class MonitorGroup(Group group)
{
    public int Id { get; set; } = group.Id;
    public string Name { get; set; } = group.Name;
    public string? AdditionalField1 { get; set; } = group.AdditionalField1;
    public string? AdditionalField2 { get; set; } = group.AdditionalField2;
    public string? AdditionalField3 { get; set; } = group.AdditionalField3;
    public string? AdditionalField4 { get; set; } = group.AdditionalField4;
    public string? AdditionalField5 { get; set; } = group.AdditionalField5;
    public IEnumerable<MonitorUser> Users { get; set; } = group.Users.Select(u => new MonitorUser(u));
}