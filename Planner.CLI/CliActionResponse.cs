using RestSharp;
using Spectre.Console;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planner.CLI
{
    public class CliActionResponse
    {
        public CliActionResponse()
        {
        }

        public CliActionResponse(RestResponse response)
        {
            Response = response;
        }

        public CliActionResponse(RestResponse response, string mesage)
            : this(response)
        {
            Message = mesage;
        }

        public CliActionResponse(RestResponse response, object serializeObj = null)
            : this(response)
        {
            if (serializeObj != null)
            {
                Message = SerializeResponse(serializeObj);
            }
        }

        public CliActionResponse(RestResponse response, Table table)
            : this(response)
        {
            Tables = new List<Table> { table };
        }

        public CliActionResponse(RestResponse response, List<Table> tables)
            : this(response)
        {
            Tables = tables;
        }

        public RestResponse Response { get; private set; }

        public string Message { get; private set; }

        public List<Table> Tables { get; private set; }

        public static CliActionResponse Empty
        {
            get
            {
                return new CliActionResponse();
            }
        }

        protected static string SerializeResponse(object response)
        {
            if (response == null) return null;
            var serializer = new SerializerBuilder().Build();
            var yml = serializer.Serialize(response);
            return yml;
        }
    }
}