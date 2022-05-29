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
    public class ParametersDomain : BaseBL<ParametersDomain>
    {
        public ParametersDomain(ILogger<ParametersDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task Delete(string key)
        {
            try
            {
                await DataLayer.RemoveGlobalParameter(key);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"Parameter with key '{key}' not found");
            }
        }

        public async Task Flush()
        {
            await MainService.LoadGlobalParameters();
        }

        public async Task<string> Get(string key)
        {
            var data = await DataLayer.GetGlobalParameter(key);

            if (data == null)
            {
                throw new RestNotFoundException($"Parameter with key '{key}' not found");
            }

            return data?.ParamValue;
        }

        public async Task<Dictionary<string, string>> GetAll()
        {
            var data = (await DataLayer.GetAllGlobalParameter())
                .Select(p => GetGlobalParameterData(p))
                .ToDictionary(p => p.Key, p => p.Value);

            return data;
        }

        public async Task Upsert(GlobalParameterData request)
        {
            var exists = await DataLayer.IsGlobalParameterExists(request.Key);
            var data = GetGlobalParameter(request);
            if (exists)
            {
                await DataLayer.UpdateGlobalParameter(data);
            }
            else
            {
                await DataLayer.AddGlobalParameter(data);
            }
        }

        private static GlobalParameter GetGlobalParameter(GlobalParameterData request)
        {
            var result = new GlobalParameter
            {
                ParamKey = request.Key,
                ParamValue = request.Value
            };

            return result;
        }

        private static GlobalParameterData GetGlobalParameterData(GlobalParameter data)
        {
            var result = new GlobalParameterData
            {
                Key = data.ParamKey,
                Value = data.ParamValue
            };

            return result;
        }
    }
}