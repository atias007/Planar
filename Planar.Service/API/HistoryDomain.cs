using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class HistoryDomain : BaseBL<HistoryDomain>
    {
        public HistoryDomain(ILogger<HistoryDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task<List<JobInstanceLogRow>> GetHistory(GetHistoryRequest request)
        {
            if (request.Rows.GetValueOrDefault() == 0) { request.Rows = 50; }
            var result = await DataLayer.GetHistory(request);
            return result;
        }

        public async Task<JobInstanceLog> GetHistoryById(int id)
        {
            var result = await DataLayer.GetHistoryById(id);
            ValidateExistingEntity(result);
            var response = JsonMapper.Map<JobInstanceLog, Model.JobInstanceLog>(result);
            return response;
        }

        public async Task<string> GetHistoryDataById(int id)
        {
            var result = await DataLayer.GetHistoryDataById(id);
            if (string.IsNullOrEmpty(result))
            {
                if (await DataLayer.IsHistoryExists(id) == false)
                {
                    throw new RestNotFoundException();
                }
            }

            return result;
        }

        public async Task<string> GetHistoryInformationById(int id)
        {
            var result = await DataLayer.GetHistoryInformationById(id);
            if (string.IsNullOrEmpty(result))
            {
                if (await DataLayer.IsHistoryExists(id) == false)
                {
                    throw new RestNotFoundException();
                }
            }

            return result;
        }

        public async Task<string> GetHistoryExceptionById(int id)
        {
            var result = await DataLayer.GetHistoryExceptionById(id);
            if (string.IsNullOrEmpty(result))
            {
                if (await DataLayer.IsHistoryExists(id) == false)
                {
                    throw new RestNotFoundException();
                }
            }

            return result;
        }

        public async Task<List<JobInstanceLogRow>> GetLastHistoryCallForJob(int lastDays)
        {
            var parameters = new { LastDays = lastDays };
            var result = (await DataLayer.GetLastHistoryCallForJob(parameters)).ToList();
            return result;
        }
    }
}