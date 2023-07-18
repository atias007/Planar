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

        public IQueryable<JobInstanceLog> GetHistory(long key)
        {
            var history = DataLayer.GetHistory(key);
            return history;
        }

        #endregion OData

        public async Task<PagingResponse<JobInstanceLogRow>> GetHistory(GetHistoryRequest request)
        {
            var query = DataLayer.GetHistory(request);
            var data = await query.ProjectToWithPagingAsyc<JobInstanceLog, JobInstanceLogRow>(Mapper, request);
            var result = new PagingResponse<JobInstanceLogRow>(data);
            return result;
        }

        public async Task<JobInstanceLog> GetHistoryById(long id)
        {
            var data = await DataLayer.GetHistoryById(id);
            var result = ValidateExistingEntity(data, "history");
            return result;
        }

        public async Task<string?> GetHistoryDataById(long id)
        {
            var result = await DataLayer.GetHistoryDataById(id);
            await ValidateHistoryExists(id, result);
            return result;
        }

        public async Task<string?> GetHistoryLogById(long id)
        {
            var result = await DataLayer.GetHistoryLogById(id);

            // generate NotFound response
            await ValidateHistoryExists(id, result);
            return result;
        }

        public async Task<string?> GetHistoryExceptionById(long id)
        {
            var result = await DataLayer.GetHistoryExceptionById(id);

            // generate NotFound response
            await ValidateHistoryExists(id, result);

            return result;
        }

        public async Task<PagingResponse<JobHistory>> GetLastHistoryCallForJob(GetLastHistoryCallForJobRequest request)
        {
            request.SetPagingDefaults();
            request.LastDays ??= 365;
            var parameters1 = new { request.LastDays, request.PageNumber, request.PageSize };
            var data = await DataLayer.GetLastHistoryCallForJob(parameters1);
            var mappedData = Mapper.Map<List<JobHistory>>(data.Data);
            var result = new PagingResponse<JobHistory>(mappedData);
            result.SetPagingData(request, data.TotalRows);
            return result;
        }

        private async Task ValidateHistoryExists(long id, string? result)
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