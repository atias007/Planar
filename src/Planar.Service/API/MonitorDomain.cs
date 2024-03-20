﻿using CommonJob;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.Monitor.Test;
using Planar.Service.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class MonitorDomain(IServiceProvider serviceProvider) : BaseBL<MonitorDomain, MonitorData>(serviceProvider)
    {
        private static readonly int[] _counterEvents = [
            (int)MonitorEvents.ClusterHealthCheckFail,
            (int)MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanx,
            (int)MonitorEvents.ExecutionEndWithEffectedRowsLessThanx,
            (int)MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanxInyHours,
            (int)MonitorEvents.ExecutionEndWithEffectedRowsLessThanxInyHours,
            (int)MonitorEvents.ExecutionFail,
            (int)MonitorEvents.ExecutionFailxTimesInRow,
            (int)MonitorEvents.ExecutionFailxTimesInyHours,
            (int)MonitorEvents.ExecutionLastRetryFail,
            (int)MonitorEvents.ExecutionSuccessWithNoEffectedRows,
            (int)MonitorEvents.ExecutionVetoed];

        public static List<MonitorEventModel> GetEvents()
        {
            var result =
                Enum.GetValues(typeof(MonitorEvents))
                .Cast<MonitorEvents>()
                .Select(e => new MonitorEventModel { EventName = e.ToString(), EventTitle = e.GetEnumDescription() })
                .OrderBy(e => e.EventName)
                .ToList();

            return result;
        }

        public async Task<int> Add(AddMonitorRequest request)
        {
            var validator = new MonitorActionValidator();
            validator.ValidateMonitorArguments(request);

            var mapperData = Resolve<AutoMapperData>();
            var monitor = Mapper.Map<MonitorAction>(request);
            monitor.GroupId = await mapperData.GetGroupId(request.GroupName);

            if (string.IsNullOrWhiteSpace(monitor.JobGroup)) { monitor.JobGroup = null; }
            if (string.IsNullOrWhiteSpace(monitor.JobName)) { monitor.JobName = null; }
            monitor.Active = true;

            if (await DataLayer.IsMonitorExists(monitor))
            {
                throw new RestConflictException("monitor with same properties already exists");
            }

            await DataLayer.AddMonitor(monitor);
            _ = Resolve<MonitorDurationCache>().Flush();
            return monitor.Id;
        }

        public async Task Delete(int id)
        {
            var monitor = new MonitorAction { Id = id };

            try
            {
                await DataLayer.DeleteMonitor(monitor);
                _ = Resolve<MonitorDurationCache>().Flush();
                AuditSecuritySafe($"monitor id {id} was deleted by user", true);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"monitor with id {id} could not be found");
            }
        }

        public async Task<PagingResponse<MonitorItem>> GetAll(IPagingRequest request)
        {
            var query = DataLayer.GetMonitorActions();
            var result = await query.ProjectToWithPagingAsyc<MonitorAction, MonitorItem>(Mapper, request);
            FillDistributionGroupName(result.Data);
            return result;
        }

        public async Task<List<MonitorItem>> GetMonitorActionsByGroup(string group)
        {
            if (!await JobKeyHelper.IsJobGroupExists(group))
            {
                throw new RestValidationException("group", $"group with name '{group}' is not exists");
            }

            var items = await DataLayer.GetMonitorActionsByGroup(group);
            var result = Mapper.Map<List<MonitorItem>>(items);
            FillDistributionGroupName(result);
            return result;
        }

        public async Task<MonitorItem> GetMonitorActionById(int id)
        {
            var item = await DataLayer.GetMonitorAction(id);
            var monitor = ValidateExistingEntity(item, "monitor");
            var result = Mapper.Map<MonitorItem>(monitor);
            FillDistributionGroupName(result);
            return result;
        }

        public async Task<List<MonitorItem>> GetByJob(string jobId)
        {
            var jobKey = await JobKeyHelper.GetJobKey(jobId);
            if (jobKey == null) { return []; }
            var items = await DataLayer.GetMonitorActionsByJob(jobKey.Group, jobKey.Name);
            var result = Mapper.Map<List<MonitorItem>>(items);
            FillDistributionGroupName(result);
            return result;
        }

        public IEnumerable<HookInfo> GetHooks()
        {
            var result = Mapper.Map<List<HookInfo>>(ServiceUtil.MonitorHooks.Values)
                .OrderBy(h => h.Name);

            return result;
        }

        public async Task<MonitorAlertModel> GetMonitorAlert(int id)
        {
            var query = DataLayer.GetMonitorAlert(id);
            var result = await Mapper.ProjectTo<MonitorAlertModel>(query).FirstOrDefaultAsync();
            ValidateExistingEntity(result, "monitor alert");
            return result!;
        }

        public async Task<MonitorItem> GetMonitorItem(int id)
        {
            var action = await DataLayer.GetMonitorAction(id);
            var item = Mapper.Map<MonitorItem>(action);
            var result = ValidateExistingEntity(item, "monitor");
            FillDistributionGroupName(result);
            return result;
        }

        public async Task<PagingResponse<MonitorAlertRowModel>> GetMonitorsAlerts(GetMonitorsAlertsRequest request)
        {
            var query = DataLayer.GetMonitorAlerts(request);
            var data = await query.ProjectToWithPagingAsyc<MonitorAlert, MonitorAlertRowModel>(Mapper, request);
            var result = new PagingResponse<MonitorAlertRowModel>(data);
            return result;
        }

        public async Task Mute(MonitorMuteRequest request)
        {
            var jobId = await ValidateUnmutedRequest(request);

            var entity = new MonitorMute
            {
                DueDate = request.DueDate,
                JobId = jobId,
                MonitorId = request.MonitorId,
            };

            await DataLayer.AddMonitorMute(entity);
            var message = $"monitor {entity.JobId ?? "[all monitors]"} with job {entity.JobId ?? "[all jobs]"}  was muted by user";
            AuditSecuritySafe(message, true);
        }

        public async Task<IEnumerable<MuteItem>> Mutes()
        {
            var mutes = await DataLayer.GetMonitorMutes();
            var counters = await DataLayer.GetMonitorCounters(AppSettings.Monitor.MaxAlertsPerMonitor);

            var mutesDto = Mapper.Map<List<MuteItem>>(mutes);
            var countersDto = Mapper.Map<List<MuteItem>>(counters);
            var all = mutesDto.Union(countersDto);
            var result = all
                .Where(i => i.DueDate > DateTime.Now)
                .GroupBy(i => new { i.JobId, i.MonitorId, i.MonitorTitle })
                .Select(g => new MuteItem
                {
                    JobId = g.Key.JobId,
                    MonitorId = g.Key.MonitorId,
                    DueDate = g.Max(i => i.DueDate),
                    MonitorTitle = g.Key.MonitorTitle
                })
                .OrderBy(i => i.DueDate)
                .Take(1000)
                .ToList();

            foreach (var r in result)
            {
                if (string.IsNullOrEmpty(r.JobId)) { continue; }
                var key = await JobKeyHelper.GetJobKeyById(r.JobId);
                r.JobName = key?.Name;
                r.JobGroup = key?.Group;
            }

            return result;
        }

        public async Task PartialUpdateMonitor(UpdateEntityRequestById request)
        {
            var dbMonitor = await DataLayer.GetMonitorAction(request.Id);
            var monitor = ValidateExistingEntity(dbMonitor, "monitor");
            ForbbidenPartialUpdateProperties(request, "EventId", "GroupId");
            var mapperData = Resolve<AutoMapperData>();
            var updateMonitor = Mapper.Map<UpdateMonitorRequest>(monitor);
            updateMonitor.GroupName = await mapperData.GetGroupName(monitor.GroupId) ?? string.Empty;
            var validator = Resolve<IValidator<UpdateMonitorRequest>>();
            await SetEntityProperties(updateMonitor, request, validator);
            await Update(updateMonitor);
        }

        public async Task<string> Reload()
        {
            ServiceUtil.LoadMonitorHooks(Logger);
            if (AppSettings.Cluster.Clustering)
            {
                await ClusterUtil.LoadMonitorHooks();
            }

            var monitor = _serviceProvider.GetRequiredService<MonitorUtil>();
            await monitor.Validate();

            return $"{ServiceUtil.MonitorHooks.Count} monitor hooks loaded";
        }

        public async Task Try(MonitorTestRequest request)
        {
            var monitorUtil = Resolve<MonitorUtil>();
            var groupDal = Resolve<GroupData>();
            var groupId = await groupDal.GetGroupId(request.GroupName ?? string.Empty);
            var group = await groupDal.GetGroupWithUsers(groupId);
            var monitorEvent = Enum.Parse<MonitorEvents>(request.EventName.ToString());
            var exception = new Exception("this is test exception");

            if (group == null)
            {
                var field = nameof(request.GroupName);
                throw new RestValidationException(field, $"{field} was not found");
            }

            var action = new MonitorAction
            {
                Active = true,
                EventArgument = null,
                EventId = (int)monitorEvent,
                Group = group,
                GroupId = groupId,
                Hook = request.Hook,
                JobGroup = "TestJobGroup",
                JobName = "TestJobName",
                Title = "Test Monitor"
            };

            if (MonitorEventsExtensions.IsSystemMonitorEvent(monitorEvent))
            {
                var info = new MonitorSystemInfo
                (
                    $"This is test monitor for system event {monitorEvent}"
                );
                await monitorUtil.ExecuteMonitor(action, monitorEvent, info, exception);
            }
            else if (MonitorEventsExtensions.IsSimpleJobMonitorEvent(monitorEvent))
            {
                var context = new TestJobExecutionContext(request);
                await monitorUtil.ExecuteMonitor(action, monitorEvent, context, exception);
            }
            else
            {
                throw new RestValidationException(nameof(MonitorTestRequest.EventName), $"monitor event '{monitorEvent}' is not supported for test");
            }
        }

        public async Task UnMute(MonitorUnmuteRequest request)
        {
            var jobId = await ValidateUnmutedRequest(request);
            var hasJobId = jobId != null;
            var hasMonitorId = request.MonitorId.HasValue;

            if (hasJobId && hasMonitorId)
            {
                await DataLayer.UnMute(jobId!, request.MonitorId.GetValueOrDefault());
            }
            else if (!hasJobId && hasMonitorId)
            {
                await DataLayer.UnMute(request.MonitorId.GetValueOrDefault());
            }
            else if (hasJobId)
            {
                await DataLayer.UnMute(jobId!);
            }
            else
            {
                await DataLayer.UnMute();
            }
        }

        public async Task Update(UpdateMonitorRequest request)
        {
            var exists = await DataLayer.IsMonitorExists(request.Id);
            if (!exists)
            {
                throw new RestNotFoundException($"monitor with id '{request.Id}' is not exists");
            }

            var validator = new MonitorActionValidator();
            validator.ValidateMonitorArguments(request);
            var mapperData = Resolve<AutoMapperData>();
            var monitor = Mapper.Map<MonitorAction>(request);
            monitor.GroupId = await mapperData.GetGroupId(request.GroupName);
            if (string.IsNullOrWhiteSpace(monitor.JobGroup)) { monitor.JobGroup = null; }
            if (string.IsNullOrWhiteSpace(monitor.JobName)) { monitor.JobName = null; }
            if (await DataLayer.IsMonitorExists(monitor, request.Id))
            {
                throw new RestConflictException("monitor with same properties already exists");
            }

            await DataLayer.UpdateMonitorAction(monitor);
            _ = Resolve<MonitorDurationCache>().Flush();
        }

        internal async Task<bool> CheckForMutedMonitor(int? eventId, string jobId, int monitorId)
        {
            // Check for auto muted monitor
            if (eventId == null || _counterEvents.Contains(eventId.GetValueOrDefault()))
            {
                var count = await DataLayer.GetMonitorCounter(jobId, monitorId, AppSettings.Monitor.MaxAlertsPeriod);
                var isAutoMuted = count >= AppSettings.Monitor.MaxAlertsPerMonitor;
                if (isAutoMuted) { return true; }
            }

            // Check for manual muted monitor
            var muted = await DataLayer.IsMonitorMuted(jobId, monitorId);
            return muted;
        }

        internal async Task SaveMonitorCounter(MonitorAction action, MonitorDetails details)
        {
            if (action.Id == 0) { return; }
            if (!_counterEvents.Contains(action.EventId)) { return; }
            if (details.JobId == null) { return; }

            var counter = new MonitorCounter
            {
                Counter = 1,
                JobId = details.JobId,
                LastUpdate = DateTime.Now,
                MonitorId = action.Id
            };

            var exists = await DataLayer.IsMonitorCounterExists(counter.JobId);
            if (exists)
            {
                await DataLayer.IncreaseMonitorCounter(counter.JobId, counter.MonitorId);
            }
            else
            {
                try
                {
                    await DataLayer.AddMonitorCounter(counter);
                }
                catch (DbUpdateException)
                {
                    await DataLayer.IncreaseMonitorCounter(counter.JobId, counter.MonitorId);
                }
            }
        }

        private void FillDistributionGroupName(IEnumerable<MonitorItem>? items)
        {
            if (items == null) { return; }
            var mappaerData = Resolve<AutoMapperData>();
            var dic = items.Select(i => i.GroupId).Distinct().ToDictionary(i => i, i => mappaerData.GetGroupName(i).Result ?? string.Empty);
            foreach (var item in items)
            {
                item.DistributionGroupName = dic[item.GroupId];
            }
        }

        private void FillDistributionGroupName(MonitorItem? item)
        {
            if (item == null) { return; }
            var mappaerData = Resolve<AutoMapperData>();
            item.DistributionGroupName = mappaerData.GetGroupName(item.GroupId).Result ?? string.Empty;
        }

        private async Task<string?> ValidateUnmutedRequest(MonitorUnmuteRequest request)
        {
            string? jobId = null;
            var hasJobId = !string.IsNullOrEmpty(request.JobId);
            var hasMonitorId = request.MonitorId.HasValue;
            if (hasJobId)
            {
                jobId = await JobKeyHelper.GetJobId(request.JobId!)
                    ?? throw new RestValidationException(nameof(request.JobId), $"job with id '{request.JobId}' is not exists");
            }

            if (hasMonitorId)
            {
                if (!await DataLayer.IsMonitorExists(request.MonitorId.GetValueOrDefault()))
                {
                    throw new RestValidationException(nameof(request.MonitorId), $"monitor id '{request.MonitorId}' is not exists");
                }

                var eventId = await DataLayer.GetMonitorEventId(request.MonitorId.GetValueOrDefault());
                if (MonitorEventsExtensions.IsSystemMonitorEvent(eventId) && hasJobId)
                {
                    throw new RestValidationException(nameof(request.JobId), $"job id is invalid for monitor id '{request.MonitorId}'. this monitor has system event so job id is not relevand");
                }
            }

            return jobId;
        }
    }
}