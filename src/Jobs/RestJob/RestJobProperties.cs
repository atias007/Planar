using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace RestJob
{
    public class RestJobProperties
    {
        public enum RestJobHttpMethods
        {
            POST,
            GET,
            PUT,
            DELETE,
            PATCH,
            HEAD
        }

        public Uri Url { get; set; } = null!;
        public RestJobHttpMethods Method { get; set; }

        [YamlMember(Alias = "ignore ssl errors")]
        public bool IgnoreSslErrors { get; set; }

        [YamlMember(Alias = "form data")]
        public Dictionary<string, string> FormData { get; set; } = new();

        public Dictionary<string, string> Headers { get; set; } = new();
    }
}