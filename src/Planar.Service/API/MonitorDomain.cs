using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.Validation;
using Quartz;
using Quartz.Impl.Matchers;
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
            var result = Mapper.Map<MonitorAction, MonitorItem>(item);
            return result;
        }

        public async Task<List<MonitorItem>> GetByKey(string key)
        {
            var items = await DataLayer.GetMonitorActionsByKey(key);
            var result = Mapper.Map<List<MonitorItem>>(items);
            return result;
        }

        public List<string> GetEvents()
        {
            var result =
                Enum.GetValues(typeof(MonitorEvents))
                .Cast<MonitorEvents>()
                .Select(e => e.ToString())
                .ToList();

            return result;
        }

        public List<string> GetHooks()
        {
            return ServiceUtil.MonitorHooks.Keys.ToList();
        }

        public async Task<MonitorActionMedatada> GetMedatada()
        {
            var result = new MonitorActionMedatada
            {
                Hooks = ServiceUtil.MonitorHooks.Keys
                    .Select((k, i) => new { k, i })
                    .ToDictionary(i => i.i + 1, k => k.k),
                Groups = await Resolve<GroupData>().GetGroupsName(),
                Events = Enum.GetValues(typeof(MonitorEvents))
                    .Cast<MonitorEvents>()
                    .ToDictionary(k => (int)k, v => v.ToString()),
            };

            var groups = (await Scheduler.GetJobGroupNames())
                .Where(g => g != Consts.PlanarSystemGroup)
                .ToList();

            if (groups.Count <= 20)
            {
                result.JobGroups = groups.ToList();
            }

            var allKeys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            if (allKeys.Count <= 20)
            {
                var jobs = allKeys
                    .ToList()
                    .Select(async key => await Scheduler.GetJobDetail(key));

                result.Jobs = jobs.ToDictionary(d => JobKeyHelper.GetJobId(d.Result), d => d.Result.Description);
            }

            return result;
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

        public async Task UpdatePartial(UpdateEntityRecord request)
        {
            var action = await DataLayer.GetMonitorAction(request.Id);
            ValidateExistingEntity(action, "monitor");
            var validator = new MonitorActionValidator(Resolve<GroupData>(), JobKeyHelper);
            await SetEntityProperties(action, request, validator);
            await DataLayer.UpdateMonitorAction(action);
        }
    }
}