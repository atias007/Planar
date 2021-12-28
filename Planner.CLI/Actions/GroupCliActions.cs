using Newtonsoft.Json;
using Planner.API.Common.Entities;
using Planner.CLI.Attributes;
using Planner.CLI.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planner.CLI.Actions
{
    [Module("group")]
    public class GroupCliActions : BaseCliAction<GroupCliActions>
    {
        [Action("add")]
        public static async Task<ActionResponse> AddGroup(CliAddGroupRequest request)
        {
            var prm = SerializeObject(request);
            var result = await Proxy.InvokeAsync(x => x.AddGroup(prm));
            return new ActionResponse(result);
        }

        [Action("get")]
        public static async Task<ActionResponse> GetGroupById(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            return await ExecuteEntity(x => x.GetGroupById(prm));
        }

        [Action("ls")]
        [Action("list")]
        public static async Task<ActionResponse> GetGroups()
        {
            var result = await Proxy.InvokeAsync(x => x.GetGroups());
            var response = JsonConvert.DeserializeObject<List<GroupRowDetails>>(result.Result);
            var table = CliTableExtensions.GetTable(result, response);
            return new ActionResponse(result, table);
        }

        [Action("remove")]
        [Action("delete")]
        public static async Task<ActionResponse> RemoveGroupById(CliGetByIdRequest request)
        {
            var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
            var result = await Proxy.InvokeAsync(x => x.RemoveGroup(prm));
            return new ActionResponse(result);
        }

        [Action("update")]
        public static async Task<ActionResponse> UpdateGroup(CliUpdateEntityRequest request)
        {
            var prm = SerializeObject(request);
            var result = await Proxy.InvokeAsync(x => x.UpdateGroup(prm));
            return new ActionResponse(result);
        }

        [Action("join")]
        public static async Task<ActionResponse> AddUserToGroup(CliUserToGroupRequest request)
        {
            var prm = SerializeObject(request);
            return await Execute(x => x.AddUserToGroup(prm));
        }

        [Action("exclude")]
        public static async Task<ActionResponse> RemoveUserFromGroup(CliUserToGroupRequest request)
        {
            var prm = SerializeObject(request);
            return await Execute(x => x.RemoveUserFromGroup(prm));
        }
    }
}