﻿using Newtonsoft.Json;
using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("group")]
    public class GroupCliActions : BaseCliAction<GroupCliActions>
    {
        [Action("add")]
        public static async Task<CliActionResponse> AddGroup(CliAddGroupRequest request)
        {
            var restRequest = new RestRequest("group", Method.Post)
                .AddBody(request);
            var result = await RestProxy.Invoke<int>(restRequest);
            return new CliActionResponse(result);
        }

        [Action("get")]
        public static async Task<CliActionResponse> GetGroupById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("group/{id}", Method.Get)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            return await ExecuteEntity(restRequest);
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<CliActionResponse> GetGroups()
        {
            var restRequest = new RestRequest("group", Method.Get);
            var result = await RestProxy.Invoke<List<GroupRowDetails>>(restRequest);

            if (result.IsSuccessful)
            {
                var table = CliTableExtensions.GetTable(result.Data);
                return new CliActionResponse(result, table);
            }

            return new CliActionResponse(result);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<CliActionResponse> RemoveGroupById(CliGetByIdRequest request)
        {
            var restRequest = new RestRequest("group/{id}", Method.Delete)
                .AddParameter("id", request.Id, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("update")]
        public static async Task<CliActionResponse> UpdateGroup(CliUpdateEntityRequest request)
        {
            var restRequest = new RestRequest("group/{id}", Method.Patch)
               .AddParameter("id", request.Id, ParameterType.UrlSegment)
               .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("join")]
        public static async Task<CliActionResponse> AddUserToGroup(CliUserToGroupRequest request)
        {
            var restRequest = new RestRequest("group/{id}/user", Method.Post)
               .AddParameter("id", request.GroupId, ParameterType.UrlSegment)
               .AddBody(request);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }

        [Action("exclude")]
        public static async Task<CliActionResponse> RemoveUserFromGroup(CliUserToGroupRequest request)
        {
            var restRequest = new RestRequest("group/{id}/user/{userId}", Method.Delete)
               .AddParameter("id", request.GroupId, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke(restRequest);
            return new CliActionResponse(result);
        }
    }
}