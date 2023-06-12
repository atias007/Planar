using System.Text.Json.Serialization;

namespace Planar.API.Common.Entities
{
    public class UpdateGroupRequest : AddGroupRequest
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public int RoleId { get; set; }

        public string CurrentName { get; set; } = null!;
    }
}