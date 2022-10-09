using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ConfigDomain : BaseBL<ConfigDomain>
    {
        public ConfigDomain(ILogger<ConfigDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task Delete(string key)
        {
            try
            {
                await DataLayer.RemoveGlobalConfig(key);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"Parameter with key '{key}' not found");
            }
        }

        public async Task Flush()
        {
            await MainService.LoadGlobalConfig();
        }

        public async Task<string> Get(string key)
        {
            var data = await DataLayer.GetGlobalConfig(key);

            if (data == null)
            {
                throw new RestNotFoundException($"Parameter with key '{key}' not found");
            }

            return data?.Value;
        }

        public async Task<Dictionary<string, string>> GetAll()
        {
            var data = (await DataLayer.GetAllGlobalConfig())
                .Select(p => GetGlobalConfigData(p))
                .ToDictionary(p => p.Key, p => p.Value);

            return data;
        }

        public async Task Upsert(GlobalConfigData request)
        {
            var exists = await DataLayer.IsGlobalConfigExists(request.Key);
            var data = GetGlobalConfig(request);
            if (exists)
            {
                await DataLayer.UpdateGlobalConfig(data);
            }
            else
            {
                await DataLayer.AddGlobalConfig(data);
            }
        }

        private static GlobalConfig GetGlobalConfig(GlobalConfigData request)
        {
            var result = new GlobalConfig
            {
                Key = request.Key,
                Value = request.Value,
                Type = request.Type
            };

            return result;
        }

        private static GlobalConfigData GetGlobalConfigData(GlobalConfig data)
        {
            var result = new GlobalConfigData
            {
                Key = data.Key,
                Value = data.Value,
                Type = data.Type
            };

            return result;
        }
    }
}