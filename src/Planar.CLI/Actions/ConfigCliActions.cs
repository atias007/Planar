using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
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
            var result = await RestProxy.Invoke<string>(restRequest);
            return new CliActionResponse(result, message: result.Data);
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
            request.Type = "string";
            var restRequest = new RestRequest("config", Method.Post)
                .AddBody(request);

            return await Execute(restRequest);
        }

        [Action("flush")]
        public static async Task<CliActionResponse> Flush()
        {
            var restRequest = new RestRequest("config/flush", Method.Post);
            return await Execute(restRequest);
        }

        [Action("remove")]
        public static async Task<CliActionResponse> RemoveConfig(CliConfigKeyRequest request)
        {
            var restRequest = new RestRequest("config/{key}", Method.Delete)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            return await Execute(restRequest);
        }
    }
}