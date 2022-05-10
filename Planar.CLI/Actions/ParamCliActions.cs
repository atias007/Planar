using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("param")]
    public class ParamCliActions : BaseCliAction<ParamCliActions>
    {
        [Action("get")]
        public static async Task<CliActionResponse> GetParameter(CliParameterKeyRequest request)
        {
            var restRequest = new RestRequest("parameters/{key}", Method.Get)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, message: result.Data);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllParameter()
        {
            var restRequest = new RestRequest("parameters", Method.Get);
            var result = await RestProxy.Invoke<Dictionary<string, string>>(restRequest);
            return new CliActionResponse(result, serializeObj: result.Data);
        }

        [Action("upsert")]
        [Action("add")]
        public static async Task<CliActionResponse> Upsert(CliParameterRequest request)
        {
            var restRequest = new RestRequest("parameters", Method.Post)
                .AddBody(request);
            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("flush")]
        public static async Task<CliActionResponse> Flush()
        {
            var restRequest = new RestRequest("parameters/flush", Method.Post);
            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("remove")]
        public static async Task<CliActionResponse> RemoveParameter(CliParameterKeyRequest request)
        {
            var restRequest = new RestRequest("parameters/{key}", Method.Delete)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }
    }
}