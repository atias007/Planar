using CommonJob;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.Monitor.Test;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            if (string.IsNullOrEmpty(monitor.JobGroup)) { monitor.JobGroup = null; }
            if (string.IsNullOrEmpty(monitor.JobName)) { monitor.JobName = null; }
            monitor.Active = true;

            if (await DataLayer.IsMonitorExists(monitor))
            {
                throw new RestConflictException("monitor with same properties already exists");
            }

            await DataLayer.AddMonitor(monitor);
            return monitor.Id;
        }

        public async Task Delete(int id)
        {
            var monitor = new MonitorAction { Id = id };

            try
            {
                await DataLayer.DeleteMonitor(monitor);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"monitor with id {id} could not be found");
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

        public async Task<List<MonitorItem>> GetByJob(string jobId)
        {
            var jobKey = await JobKeyHelper.GetJobKey(jobId);
            var items = await DataLayer.GetMonitorActionsByJob(jobKey.Group, jobKey.Name);
            var result = Mapper.Map<List<MonitorItem>>(items);
            return result;
        }

        public async Task<List<MonitorItem>> GetByGroup(string group)
        {
            if (!await JobKeyHelper.IsJobGroupExists(group))
            {
                throw new RestValidationException("group", $"group with name '{group}' is not exists");
            }

            var items = await DataLayer.GetMonitorActionsByGroup(group);
            var result = Mapper.Map<List<MonitorItem>>(items);
            return result;
        }

        public List<LovItem> GetEvents()
        {
            var result =
                Enum.GetValues(typeof(MonitorEvents))
                .Cast<MonitorEvents>()
                .Select(e => new LovItem { Id = (int)e, Name = e.ToString().SplitWords() })
                .ToList();

            return result;
        }

        public List<string> GetHooks()
        {
            return ServiceUtil.MonitorHooks.Keys.ToList();
        }

        public async Task<string> Reload()
        {
            ServiceUtil.LoadMonitorHooks(Logger);
            if (AppSettings.Clustering)
            {
                await ClusterUtil.LoadMonitorHooks();
            }

            var monitor = _serviceProvider.GetRequiredService<MonitorUtil>();
            await monitor.Validate();

            return $"{ServiceUtil.MonitorHooks.Count} monitor hooks loaded";
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
        }

        public async Task PartialUpdateMonitor(UpdateEntityRequest request)
        {
            var monitor = await DataLayer.GetMonitorAction(request.Id);
            ValidateExistingEntity(monitor, "monitor");
            var updateMonitor = Mapper.Map<UpdateMonitorRequest>(monitor);
            var validator = Resolve<IValidator<UpdateMonitorRequest>>();
            await SetEntityProperties(updateMonitor, request, validator);
            await Update(updateMonitor);
        }

        public async Task Test(MonitorTestRequest request)
        {
            var monitorUtil = Resolve<MonitorUtil>();
            var groupDal = Resolve<GroupData>();
            var group = await groupDal.GetGroup(request.DistributionGroupId);
            var monitorEvent = Enum.Parse<MonitorEvents>(request.MonitorEvent.ToString());
            var exception = new Exception("This is test exception");
            var action = new MonitorAction
            {
                Active = true,
                EventArgument = null,
                EventId = (int)request.MonitorEvent,
                Group = group,
                GroupId = request.DistributionGroupId,
                Hook = request.Hook,
                JobGroup = "TestJobGroup",
                JobName = "TestJobName",
                Title = "Test Monitor"
            };

            if (MonitorEventsExtensions.IsSystemMonitorEvent(monitorEvent))
            {
                var info = new MonitorSystemInfo
                {
                    MessageTemplate = $"This is test monitor for system event {monitorEvent}"
                };
                await monitorUtil.ExecuteMonitor(action, monitorEvent, info, exception);
            }
            else if (MonitorEventsExtensions.IsSimpleJobMonitorEvent(monitorEvent))
            {
                var context = new TestJobExecutionContext(request);
                await monitorUtil.ExecuteMonitor(action, monitorEvent, context, exception);
            }
            else
            {
                throw new RestValidationException(nameof(MonitorTestRequest.MonitorEvent), $"monitor enent '{monitorEvent}' is not supported for test");
            }
        }
    }
}