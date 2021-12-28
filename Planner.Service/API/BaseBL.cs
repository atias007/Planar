using Newtonsoft.Json;
using Planner.API.Common.Entities;
using System;
using System.Threading.Tasks;

namespace Planner.Service.API
{
    public abstract class BaseBL
    {
        protected static async Task<BaseResponse<string>> GetBaseResponse(Func<Task<object>> func)
        {
            var entity = await func();
            var json = JsonConvert.SerializeObject(entity);
            var result = new BaseResponse<string>(json);
            return result;
        }

        protected static async Task<BaseResponse> GetBaseResponse(Func<Task> func)
        {
            await func();
            var result = BaseResponse.Empty;
            return result;
        }

        protected static T DeserializeObject<T>(string json)
        {
            var entity = JsonConvert.DeserializeObject<T>(json);
            return entity;
        }

        protected static string SerializeObject(object obj)
        {
            var entity = JsonConvert.SerializeObject(obj);
            return entity;
        }
    }
}