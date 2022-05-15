using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("user")]
    public class UserCliActions : BaseCliAction<UserCliActions>
    {
        ////[Action("add")]
        ////public static async Task<ActionResponse> AddUser()
        ////{
        ////    var prm = CollectDataFromCli<AddUserRequest>();
        ////    var result = await Proxy.InvokeAsync(x => x.AddUser(prm));
        ////    return new ActionResponse(result, serializeObj: result.Result);
        ////}

        ////[Action("get")]
        ////public static async Task<ActionResponse> GetUserById(CliGetByIdRequest request)
        ////{
        ////    var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);

        ////    return await ExecuteEntity(x => x.GetUser(prm));
        ////}

        ////[Action("ls")]
        ////[Action("list")]
        ////public static async Task<ActionResponse> GetUsers()
        ////{
        ////    var result = await Proxy.InvokeAsync(x => x.GetUsers());
        ////    var response = JsonConvert.DeserializeObject<List<UserRowDetails>>(result.Result);
        ////    var table = CliTableExtensions.GetTable(result, response);
        ////    return new ActionResponse(result, table);
        ////}

        ////[Action("remove")]
        ////[Action("delete")]
        ////public static async Task<ActionResponse> RemoveUserById(CliGetByIdRequest request)
        ////{
        ////    var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
        ////    var result = await Proxy.InvokeAsync(x => x.RemoveUser(prm));
        ////    return new ActionResponse(result);
        ////}

        ////[Action("update")]
        ////public static async Task<ActionResponse> UpdateUser(CliUpdateEntityRequest request)
        ////{
        ////    var prm = JsonConvert.SerializeObject(request);
        ////    var result = await Proxy.InvokeAsync(x => x.UpdateUser(prm));
        ////    return new ActionResponse(result);
        ////}

        ////[Action("password")]
        ////public static async Task<ActionResponse> GetUserPassword(CliGetByIdRequest request)
        ////{
        ////    var prm = JsonMapper.Map<GetByIdRequest, CliGetByIdRequest>(request);
        ////    var result = await Proxy.InvokeAsync(x => x.GetUserPassword(prm));
        ////    return new ActionResponse(result, serializeObj: result.Result);
        ////}
    }
}