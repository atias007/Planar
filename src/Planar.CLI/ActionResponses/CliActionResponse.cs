using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Planar.Common;
using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Planar.CLI
{
    public class CliActionResponse
    {
        public CliActionResponse(RestResponse? response)
        {
            response ??= GetGenericSuccessRestResponse();

            Response = response;
        }

        public CliActionResponse(RestResponse? response, string? message, bool? formattedMessage = false)
            : this(response)
        {
            Message = message;
            FormattedMessage = formattedMessage;
        }

        public CliActionResponse(RestResponse? response, CliTable table)
            : this(response)
        {
            Tables = [table];
        }

        public CliActionResponse(RestResponse? response, List<CliTable> tables)
            : this(response)
        {
            Tables = tables;
        }

        public CliActionResponse(RestResponse? response, CliPlot plot)
            : this(response)
        {
            Plot = plot;
        }

        public CliActionResponse(RestResponse? response, object? dumpObject)
            : this(response)
        {
            DumpObjects = [new(dumpObject)];

            if (dumpObject != null)
            {
                Message = SerializeResponse(dumpObject);
            }
        }

        public CliActionResponse(RestResponse? response, CliDumpObject dumpObject)
            : this(response)
        {
            DumpObjects = [dumpObject];

            if (dumpObject != null)
            {
                Message = SerializeResponse(dumpObject);
            }
        }

        public CliActionResponse(RestResponse? response, List<CliDumpObject> dumpObjects)
            : this(response)
        {
            DumpObjects = dumpObjects;

            if (dumpObjects != null)
            {
                Message = SerializeResponse(dumpObjects);
            }
        }

        public RestResponse Response { get; private set; }
        public string? Message { get; private set; }
        public bool? FormattedMessage { get; private set; }
        public List<CliDumpObject>? DumpObjects { get; private set; }
        public List<CliTable>? Tables { get; private set; }
        public CliPlot? Plot { get; private set; }

        public static CliActionResponse Empty
        {
            get
            {
                return new CliActionResponse(null);
            }
        }

        public IPagingResponse? GetPagingResponse()
        {
            if (string.IsNullOrEmpty(Response.Content)) { return null; }

            try
            {
                var result = JsonConvert.DeserializeObject<PagingResponse>(Response.Content);
                return result;
            }
            catch
            {
                return null;
            }
        }

        internal static RestResponse GetGenericSuccessRestResponse()
        {
            var response = new RestResponse { StatusCode = HttpStatusCode.OK, ResponseStatus = ResponseStatus.Completed, IsSuccessStatusCode = true };
            return response;
        }

        internal async static Task<RestResponse> Convert(HttpResponseMessage response)
        {
            var result = new RestResponse
            {
                StatusCode = response.StatusCode,
                ResponseStatus = ResponseStatus.Completed,
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                Content = await response.Content.ReadAsStringAsync(),
                RawBytes = await response.Content.ReadAsByteArrayAsync(),
            };

            return result;
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