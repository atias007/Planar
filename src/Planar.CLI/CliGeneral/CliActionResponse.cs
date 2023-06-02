using Planar.CLI.CliGeneral;
using Planar.Common;
using RestSharp;
using Spectre.Console;
using System.Collections.Generic;
using System.Net;

namespace Planar.CLI
{
    public class CliActionResponse
    {
        public CliActionResponse(RestResponse? response)
        {
            response ??= GetGenericSuccessRestResponse();

            Response = response;
        }

        public CliActionResponse(RestResponse? response, string? message)
            : this(response)
        {
            Message = message;
        }

        public CliActionResponse(RestResponse? response, CliTable table)
            : this(response)
        {
            Tables = new List<CliTable> { table };
        }

        public CliActionResponse(RestResponse? response, object? dumpObject)
            : this(response)
        {
            DumpObject = dumpObject;

            if (dumpObject != null)
            {
                Message = SerializeResponse(dumpObject);
            }
        }

        public CliActionResponse(RestResponse? response, List<CliTable> tables)
            : this(response)
        {
            Tables = tables;
        }

        public RestResponse Response { get; private set; }

        public string? Message { get; private set; }

        public object? DumpObject { get; private set; }

        public List<CliTable>? Tables { get; private set; }

        public static CliActionResponse Empty
        {
            get
            {
                return new CliActionResponse(null);
            }
        }

        internal static RestResponse GetGenericSuccessRestResponse()
        {
            var response = new RestResponse { StatusCode = HttpStatusCode.OK, ResponseStatus = ResponseStatus.Completed, IsSuccessStatusCode = true };
            return response;
        }

        private static string? SerializeResponse(object response)
        {
            if (response == null) { return null; }
            var yml = YmlUtil.Serialize(response);

            if (!string.IsNullOrEmpty(yml))
            {
                yml = yml.Trim();
            }

            if (yml == "{}")
            {
                yml = null;
            }

            return yml;
        }
    }
}