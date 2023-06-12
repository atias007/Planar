using System.Text.Json.Serialization;

namespace Planar.API.Common.Entities
{
    public class UpdateUserRequest : AddUserRequest
    {
        [JsonIgnore]
        public int Id { get; set; }

        public string CurrentUsername { get; set; } = null!;
    }
}