using Planar.API.Common.Entities;
using Spectre.Console;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.CLI
{
    public class ActionResponse
    {
        public ActionResponse(BaseResponse response, string mesage)
            : this(response)
        {
            Message = mesage;
        }

        public ActionResponse(BaseResponse response, object serializeObj = null)
        {
            Response = response;

            if (serializeObj != null)
            {
                Message = SerializeResponse(serializeObj);
            }
        }

        public ActionResponse(BaseResponse response, Table table)
            : this(response)
        {
            Tables = new List<Table> { table };
        }

        public ActionResponse(BaseResponse response, List<Table> tables)
            : this(response)
        {
            Tables = tables;
        }

        public BaseResponse Response { get; private set; }

        public string Message { get; private set; }

        public List<Table> Tables { get; private set; }

        public static ActionResponse Empty
        {
            get
            {
                return new ActionResponse(BaseResponse.Empty);
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