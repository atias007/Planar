using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
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
    public class MonitorDomain : BaseBL<MonitorDomain>
    {
        public MonitorDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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

        public List<string> GetHooks()
        {
            return ServiceUtil.MonitorHooks.Keys.ToList();
        }

        public async Task<List<MonitorItem>> Get(string jobOrGroupId)
        {
            var items = await DataLayer.GetMonitorActions(jobOrGroupId);
            var result = items.Select(m => new MonitorItem
            {
                Active = m.Active.GetValueOrDefault(),
                EventTitle = ((MonitorEvents)m.EventId).ToString(),
                GroupName = m.Group.Name,
                Hook = m.Hook,
                Id = m.Id,
                Job = string.IsNullOrEmpty(m.JobGroup) ? $"Id: {m.JobId}" : $"Group: {m.JobGroup}",
                Title = m.Title
            })
            .ToList();

            return result;
        }

        public async Task<MonitorActionMedatada> GetMedatada()
        {
            var result = new MonitorActionMedatada
            {
                Hooks = ServiceUtil.MonitorHooks.Keys
                    .Select((k, i) => new { k, i })
                    .ToDictionary(i => i.i + 1, k => k.k),
                Groups = await DataLayer.GetGroupsName(),
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

        public List<string> GetEvents()
        {
            var result =
                Enum.GetValues(typeof(MonitorEvents))
                .Cast<MonitorEvents>()
                .Select(e => e.ToString())
                .ToList();

            return result;
        }

        public async Task<int> Add(AddMonitorRequest request)
        {
            var monitor = new MonitorAction
            {
                Active = true,
                EventArgument = string.IsNullOrEmpty(request.EventArguments) ? null : request.EventArguments,
                EventId = request.MonitorEvent,
                GroupId = request.GroupId,
                Hook = request.Hook,
                JobGroup = string.IsNullOrEmpty(request.JobGroup) ? null : request.JobGroup,
                JobId = request.JobId,
                Title = request.Title,
            };

            await DataLayer.AddMonitor(monitor);
            return monitor.Id;
        }

        public async Task Update(int id, UpdateEntityRecord request)
        {
            ValidateIdConflict(id, request.Id);
            ValidateForbiddenUpdateProperties(request, "Id", "JobGroup", "JobGroup", "Group", "Event");
            var action = await DataLayer.GetMonitorAction(id);
            ValidateExistingEntity(action);
            var validator = new MonitorActionValidator(DataLayer, JobKeyHelper);
            await UpdateEntity(action, request, validator);
            await DataLayer.UpdateMonitorAction(action);
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
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"{nameof(Monitor)} with id {id} could not be found");
            }
        }
    }
}