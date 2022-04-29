using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobInstanceLog : JobInstanceLogRow
    {
        [YamlMember(Order = 997)]
        public string Data { get; set; }

        [YamlMember(Order = 998)]
        public string Information { get; set; }

        [YamlMember(Order = 999)]
        public string Exception { get; set; }
    }
}