using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public abstract class OldBaseBL
    {
        public OldBaseBL()
        {
        }

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

    public abstract class OldBaseBL<T> : OldBaseBL
    {
        private readonly DataLayer _dataLayer;
        private readonly ILogger<T> _logger;

        public OldBaseBL(DataLayer dataLayer, ILogger<T> logger)
        {
            _dataLayer = dataLayer ?? throw new NullReferenceException(nameof(dataLayer));
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
        }

        protected DataLayer DataLayer => _dataLayer;

        protected ILogger<T> Logger => _logger;
    }
}