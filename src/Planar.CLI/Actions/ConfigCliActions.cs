using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("config", "Actions to add, remove, list & update global parameters", Synonyms = "configs")]
    public class ConfigCliActions : BaseCliAction<ConfigCliActions>
    {
        [Action("get")]
        public static async Task<CliActionResponse> GetConfig(CliConfigKeyRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("config/{key}", Method.Get)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            var result = await RestProxy.Invoke<CliGlobalConfig>(restRequest, cancellationToken);

            return new CliActionResponse(result, message: result.Data?.Value);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetAllConfiguration(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("config", Method.Get);
            return await ExecuteTable<List<CliGlobalConfig>>(restRequest, CliTableExtensions.GetTable, cancellationToken);
        }

        [Action("add")]
        public static async Task<CliActionResponse> Add(CliConfigRequest request, CancellationToken cancellationToken = default)
        {
            var data = new { request.Key, request.Value, Type = "string" };
            var restRequest = new RestRequest("config", Method.Post)
                .AddBody(data);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("put")]
        public static async Task<CliActionResponse> Put(CliConfigRequest request, CancellationToken cancellationToken = default)
        {
            var data = new { request.Key, request.Value, Type = "string" };
            var restRequest = new RestRequest("config", Method.Put)
                .AddBody(data);

            var result = await RestProxy.Invoke(restRequest, cancellationToken);
            return new CliActionResponse(result);
        }

        [Action("flush")]
        public static async Task<CliActionResponse> Flush(CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("config/flush", Method.Post);
            return await Execute(restRequest, cancellationToken);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveConfig(CliConfigKeyRequest request, CancellationToken cancellationToken = default)
        {
            if (!ConfirmAction($"remove config '{request.Key}'")) { return CliActionResponse.Empty; }

            var restRequest = new RestRequest("config/{key}", Method.Delete)
                .AddParameter("key", request.Key, ParameterType.UrlSegment);
            return await Execute(restRequest, cancellationToken);
        }
    }
}