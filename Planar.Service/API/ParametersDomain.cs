using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Model;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ParametersDomain : BaseBL<ParametersDomain>
    {
        public ParametersDomain(ILogger<ParametersDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task UpsertGlobalParameter(GlobalParameterData request)
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
    }
}