using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobInstanceLog : JobInstanceLogRow
    {
        [YamlMember(Order = 997)]
        public string Data { get; set; }

        [YamlMember(Order = 998)]
        public string Log { get; set; }

        [YamlMember(Order = 999)]
        public string Exception { get; set; }
    }
}