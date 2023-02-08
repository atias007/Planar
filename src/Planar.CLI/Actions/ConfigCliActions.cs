using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("config")]
    public class ConfigCliActions : BaseCliAction<ConfigCliActions>
    {
        [Action("get")]
        public static async Task<CliActionResponse> GetConfig(CliConfigKeyRequest request)
        {
            var restRequest = new RestRequest("config/{key}", Method.Get)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke<CliGlobalConfig>(restRequest);

            return new CliActionResponse(result, message: result.Data?.Value);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllConfiguration()
        {
            var restRequest = new RestRequest("config", Method.Get);
            return await ExecuteTable<List<CliGlobalConfig>>(restRequest, CliTableExtensions.GetTable);
        }

        [Action("upsert")]
        [Action("add")]
        public static async Task<CliActionResponse> Upsert(CliConfigRequest request)
        {
            var data = new { request.Key, request.Value, Type = "string" };
            var restRequest = new RestRequest("config", Method.Post)
                .AddBody(data);

            var result = await RestProxy.Invoke(restRequest);

            if (result.StatusCode == HttpStatusCode.Conflict)
            {
                restRequest = new RestRequest("config", Method.Put)
                    .AddBody(request);
                result = await RestProxy.Invoke(restRequest);
            }

            return new CliActionResponse(result);
        }

        [Action("flush")]
        public static async Task<CliActionResponse> Flush()
        {
            var restRequest = new RestRequest("config/flush", Method.Post);
            return await Execute(restRequest);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveConfig(CliConfigKeyRequest request)
        {
            if (!ConfirmAction($"remove config '{request.Key}'")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("config/{key}", Method.Delete)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            return await Execute(restRequest);
        }
    }
}