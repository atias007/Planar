using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
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
            return result;
        }

        public async Task<string> GetProperties(int id)
        {
            var result = await DataLayer.GetTraceProperties(id);
            return result;
        }
    }
}