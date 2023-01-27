using Planar.CLI.Entities;

namespace Planar.CLI.DataProtect
{
    public class UserMetadata
    {
        public string LoginName { get; set; } = string.Empty;

        public CliLoginRequest? LoginRequest { get; set; }
    }
}