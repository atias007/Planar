using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
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
        public MonitorDomain(ILogger<MonitorDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public string Reload()
        {
            var sb = new StringBuilder();

            ServiceUtil.LoadMonitorHooks(Logger);
            sb.AppendLine($"{ServiceUtil.MonitorHooks.Count} monitor hooks loaded");
            MonitorUtil.Load();
            sb.AppendLine($"{MonitorUtil.Count} monitor items loaded");
            MonitorUtil.Validate(Logger);

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

                result.Jobs = jobs.ToDictionary(d => Convert.ToString(d.Result.JobDataMap[Consts.JobId]), d => d.Result.Description);
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

            var eventExists = Enum.IsDefined(typeof(MonitorEvents), request.MonitorEvent);
            if (eventExists == false)
            {
                throw new PlanarValidationException($"Monitor event {request.MonitorEvent} does not exists");
            }

            if (string.IsNullOrEmpty(request.JobId) && string.IsNullOrEmpty(request.JobGroup))
            {
                throw new PlanarValidationException("Job id and Job group name are both missing. to add monitor please supply one of them");
            }

            if (string.IsNullOrEmpty(request.JobId) == false && string.IsNullOrEmpty(request.JobGroup) == false)
            {
                throw new PlanarValidationException("Job id and Job group name are both has value. to add monitor please supply only one of them");
            }

            if (string.IsNullOrEmpty(request.JobId) == false)
            {
                await JobKeyHelper.GetJobKey(new JobOrTriggerKey { Id = request.JobId });
            }

            var existsGroup = await DataLayer.IsGroupExists(request.GroupId);
            if (existsGroup == false)
            {
                throw new PlanarValidationException($"Group id {request.GroupId} does not exists");
            }

            var existsHook = ServiceUtil.MonitorHooks.ContainsKey(request.Hook);
            if (existsHook == false)
            {
                throw new PlanarValidationException($"Hook {request.Hook} does not exists");
            }

            if (string.IsNullOrEmpty(request.Title))
            {
                throw new PlanarValidationException("Title is null or empty");
            }

            if (request.Title?.Length > 50)
            {
                throw new PlanarValidationException($"Title lenght is {request.Title.Length}. Max lenght should be 50");
            }

            if (request.EventArguments?.Length > 50)
            {
                throw new PlanarValidationException($"Event arguments lenght is {request.EventArguments.Length}. Max lenght should be 50");
            }

            await DataLayer.AddMonitor(monitor);
            return monitor.Id;
        }

        public async Task Delete(int id)
        {
            if (id <= 0)
            {
                throw new PlanarValidationException("Id parameter must be greater then 0");
            }

            var monitor = new MonitorAction { Id = id };

            try
            {
                await DataLayer.DeleteMonitor(monitor);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new PlanarValidationException($"Monitor with id {id} could not be found", ex);
            }
        }
    }
}