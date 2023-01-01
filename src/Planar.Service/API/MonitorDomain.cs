using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class MonitorDomain : BaseBL<MonitorDomain, MonitorData>
    {
        public MonitorDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<int> Add(AddMonitorRequest request)
        {
            var monitor = Mapper.Map<MonitorAction>(request);
            await DataLayer.AddMonitor(monitor);
            ServiceUtil.LoadMonitorHooks(Logger);
            return monitor.Id;
        }

        public async Task Delete(int id)
        {
            if (id <= 0)
            {
                throw new RestValidationException("id", "id parameter must be greater then 0");
            }

            var monitor = new MonitorAction { Id = id };

            try
            {
                await DataLayer.DeleteMonitor(monitor);
                ServiceUtil.LoadMonitorHooks(Logger);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"{nameof(Monitor)} with id {id} could not be found");
            }
        }

        public async Task<List<MonitorItem>> GetAll()
        {
            var items = await DataLayer.GetMonitorActions();
            var result = Mapper.Map<List<MonitorItem>>(items);
            return result;
        }

        public async Task<MonitorItem> GetById(int id)
        {
            var item = await DataLayer.GetMonitorAction(id);
            ValidateExistingEntity(item, "monitor");
            var result = Mapper.Map<MonitorAction, MonitorItem>(item);
            return result;
        }

        public async Task<List<MonitorItem>> GetByJob(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var items = await DataLayer.GetMonitorActionsByJob(jobKey.Group, jobKey.Name);
            var result = Mapper.Map<List<MonitorItem>>(items);
            return result;
        }

        public async Task<List<MonitorItem>> GetByGroup(string group)
        {
            var items = await DataLayer.GetMonitorActionsByGroup(group);
            var result = Mapper.Map<List<MonitorItem>>(items);
            return result;
        }

        public List<LovItem> GetEvents()
        {
            var result =
                Enum.GetValues(typeof(MonitorEvents))
                .Cast<MonitorEvents>()
                .Select(e => new LovItem { Id = (int)e, Name = e.ToString() })
                .ToList();

            return result;
        }

        public List<string> GetHooks()
        {
            return ServiceUtil.MonitorHooks.Keys.ToList();
        }

        public async Task<string> Reload()
        {
            var sb = new StringBuilder();

            ServiceUtil.LoadMonitorHooks(Logger);
            sb.AppendLine($"{ServiceUtil.MonitorHooks.Count} monitor hooks loaded");
            var monitor = _serviceProvider.GetRequiredService<MonitorUtil>();
            await monitor.Validate();

            return sb.ToString();
        }

        public async Task Update(UpdateMonitorRequest request)
        {
            var exists = await DataLayer.IsMonitorExists(request.Id);
            if (!exists)
            {
                throw new RestNotFoundException($"monitor with id {request.Id} is not exists");
            }

            var monitor = Mapper.Map<MonitorAction>(request);
            await DataLayer.UpdateMonitorAction(monitor);
            ServiceUtil.LoadMonitorHooks(Logger);
        }

        public async Task PartialUpdateMonitor(UpdateEntityRecord request)
        {
            var monitor = await DataLayer.GetMonitorAction(request.Id);
            ValidateExistingEntity(monitor, "monitor");
            var updateMonitor = Mapper.Map<UpdateMonitorRequest>(monitor);
            var validator = Resolve<IValidator<UpdateMonitorRequest>>();
            await SetEntityProperties(updateMonitor, request, validator);
            await Update(updateMonitor);
        }
    }
}