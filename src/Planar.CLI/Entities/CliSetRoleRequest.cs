using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetRoleRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string Name { get; set; } = null!;

        [ActionProperty(DefaultOrder = 1)]
        public string Role { get; set; }
    }
}