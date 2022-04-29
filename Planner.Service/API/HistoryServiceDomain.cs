using Microsoft.Extensions.Logging;
using Planner.API.Common.Entities;
using Planner.Service.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planner.Service.API
{
    public class HistoryServiceDomain : BaseBL<HistoryServiceDomain>
    {
        public HistoryServiceDomain(DataLayer dataLayer, ILogger<HistoryServiceDomain> logger) : base(dataLayer, logger)
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
            var response = JsonMapper.Map<JobInstanceLog, Model.JobInstanceLog>(result);
            return response;
        }

        public async Task<string> GetHistoryDataById(int id)
        {
            var result = await DataLayer.GetHistoryDataById(id);
            return result;
        }

        public async Task<string> GetHistoryInformationById(int id)
        {
            var result = await DataLayer.GetHistoryInformationById(id);
            return result;
        }

        public async Task<string> GetHistoryExceptionById(int id)
        {
            var result = await DataLayer.GetHistoryExceptionById(id);
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