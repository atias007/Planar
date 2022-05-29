using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class TraceDomain : BaseBL<TraceDomain>
    {
        public TraceDomain(ILogger<TraceDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task<List<LogDetails>> Get(GetTraceRequest request)
        {
            if (request.Rows.GetValueOrDefault() == 0) { request.Rows = 50; }
            var result = await DataLayer.GetTrace(request);
            return result;
        }

        public async Task<string> GetException(int id)
        {
            var result = await DataLayer.GetTraceException(id);

            if (result == null)
            {
                if (await DataLayer.IsTraceExists(id) == false)
                {
                    throw new RestNotFoundException($"Trace with id {id} not found");
                }
            }

            return result;
        }

        public async Task<string> GetProperties(int id)
        {
            var result = await DataLayer.GetTraceProperties(id);

            if (result == null)
            {
                if (await DataLayer.IsTraceExists(id) == false)
                {
                    throw new RestNotFoundException($"Trace with id {id} not found");
                }
            }

            return result;
        }
    }
}