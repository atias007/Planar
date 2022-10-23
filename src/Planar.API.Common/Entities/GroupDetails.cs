using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class GroupDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public string Reference3 { get; set; }
        public string Reference4 { get; set; }
        public string Reference5 { get; set; }
        public string Role { get; set; }

        public List<string> Users { get; set; } = new();
    }
}