using Microsoft.EntityFrameworkCore;
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
    public class HistoryDomain : BaseBL<HistoryDomain, HistoryData>
    {
        public HistoryDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        #region OData

        public IQueryable<JobInstanceLog> GetHistoryData()
        {
            return DataLayer.GetHistoryData();
        }

        public IQueryable<JobInstanceLog> GetHistory(int key)
        {
            var history = DataLayer.GetHistory(key);
            return history;
        }

        #endregion OData

        public async Task<List<JobInstanceLogRow>> GetHistory(GetHistoryRequest request)
        {
            if (request.Rows.GetValueOrDefault() == 0) { request.Rows = 50; }
            if (request.JobId != null)
            {
                request.JobId = await JobKeyHelper.GetJobId(request.JobId);
            }

            var query = DataLayer.GetHistory(request);
            var result = await Mapper.ProjectTo<JobInstanceLogRow>(query).ToListAsync();
            return result;
        }

        public async Task<JobInstanceLog> GetHistoryById(int id)
        {
            var result = await DataLayer.GetHistoryById(id);
            ValidateExistingEntity(result, "history");
            return result;
        }

        public async Task<string> GetHistoryDataById(int id)
        {
            var result = await DataLayer.GetHistoryDataById(id);

            // generate NotFound response
            await ValidateHistoryExists(id, result);
            return result;
        }

        public async Task<string> GetHistoryLogById(int id)
        {
            var result = await DataLayer.GetHistoryLogById(id);

            // generate NotFound response
            await ValidateHistoryExists(id, result);
            return result;
        }

        public async Task<string> GetHistoryExceptionById(int id)
        {
            var result = await DataLayer.GetHistoryExceptionById(id);

            // generate NotFound response
            await ValidateHistoryExists(id, result);

            return result;
        }

        public async Task<List<JobInstanceLog>> GetLastHistoryCallForJob(int lastDays)
        {
            var parameters = new { LastDays = lastDays };
            var result = (await DataLayer.GetLastHistoryCallForJob(parameters)).ToList();
            return result;
        }

        private async Task ValidateHistoryExists(int id, string result)
        {
            if (string.IsNullOrEmpty(result) && !await DataLayer.IsHistoryExists(id))
            {
                throw new RestNotFoundException($"history with id {id} not found");
            }
        }

        public async Task<CounterResponse> GetHistoryCounter(CounterRequest request)
        {
            var result = new CounterResponse();
            var data = await DataLayer.GetHistoryCounter(request.Hours);
            var list = new List<StatisticsCountItem>
            {
                new StatisticsCountItem { Label = nameof(data.Running), Count = data.Running },
                new StatisticsCountItem { Label = nameof(data.Success), Count = data.Success },
                new StatisticsCountItem { Label = nameof(data.Fail), Count = data.Fail }
            };
            result.Counter = list;
            return result;
        }
    }
}