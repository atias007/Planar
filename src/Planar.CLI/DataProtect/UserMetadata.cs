using System.Collections.Generic;

namespace Planar.CLI.DataProtect
{
    public class UserMetadata
    {
        public List<LoginData> Logins { get; set; } = new();
    }
}