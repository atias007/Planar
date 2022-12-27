﻿using Planar.API.Common.Entities;
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

        public IQueryable<JobInstanceLog> GetHistoryData()
        {
            return DataLayer.GetHistoryData();
        }

        public IQueryable<JobInstanceLog> GetHistory(int key)
        {
            var history = DataLayer.GetHistory(key);

            if (history == null)
            {
                throw new RestNotFoundException();
            }

            return history;
        }

        public async Task<List<JobInstanceLog>> GetHistory(GetHistoryRequest request)
        {
            if (request.Rows.GetValueOrDefault() == 0) { request.Rows = 50; }
            var result = await DataLayer.GetHistory(request);
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
            if (string.IsNullOrEmpty(result))
            {
                if (await DataLayer.IsHistoryExists(id) == false)
                {
                    throw new RestNotFoundException($"history with id {id} not found");
                }
            }

            return result;
        }

        public async Task<string> GetHistoryLogById(int id)
        {
            var result = await DataLayer.GetHistoryLogById(id);
            if (string.IsNullOrEmpty(result))
            {
                if (await DataLayer.IsHistoryExists(id) == false)
                {
                    throw new RestNotFoundException($"history with id {id} not found");
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
                    throw new RestNotFoundException($"History with id {id} not found");
                }
            }

            return result;
        }

        public async Task<List<JobInstanceLog>> GetLastHistoryCallForJob(int lastDays)
        {
            var parameters = new { LastDays = lastDays };
            var result = (await DataLayer.GetLastHistoryCallForJob(parameters)).ToList();
            return result;
        }
    }
}