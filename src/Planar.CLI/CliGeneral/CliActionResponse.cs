using Planar.CLI.Entities;
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

        public CliActionResponse(RestResponse? response, string? message, CliOutputFilenameRequest? request = null)
            : this(response)
        {
            Message = message;
            if (request != null && request.HasOutputFilename)
            {
                OutputFilename = request.OutputFilename;
            }
        }

        public CliActionResponse(RestResponse? response, object? serializeObj, CliOutputFilenameRequest? request = null)
            : this(response)
        {
            if (serializeObj != null)
            {
                Message = SerializeResponse(serializeObj);
            }

            if (request != null && request.HasOutputFilename)
            {
                OutputFilename = request.OutputFilename;
            }
        }

        public CliActionResponse(RestResponse? response, Table table)
            : this(response)
        {
            Tables = new List<Table> { table };
        }

        public CliActionResponse(RestResponse? response, List<Table> tables)
            : this(response)
        {
            Tables = tables;
        }

        public RestResponse Response { get; private set; }

        public string? Message { get; private set; }

        public string? OutputFilename { get; private set; }

        public List<Table>? Tables { get; private set; }

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