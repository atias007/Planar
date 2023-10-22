using Planar.Api.Common.Entities;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.SystemJobs;
using Quartz;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ReportDomain : BaseBL<ReportDomain, ReportData>
    {
        public ReportDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task Update(UpdateSummaryReportRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Group))
            {
                await ValidateGroupAndEmails(request.Group);
            }

            var requestPeriod = Enum.Parse<SummaryReportPeriods>(request.Period, true);
            var triggerKey = new TriggerKey(requestPeriod.ToString(), Consts.PlanarSystemGroup);
            var scheduler = Resolve<IScheduler>();
            var trigger = await scheduler.GetTrigger(triggerKey);
            var triggerId = TriggerHelper.GetTriggerId(trigger);

            if (trigger == null || string.IsNullOrEmpty(triggerId))
            {
                throw new InvalidOperationException($"trigger with id '{triggerId}' is not exists");
            }

            var groupDal = Resolve<GroupData>();
            var groupName =
                string.IsNullOrWhiteSpace(request.Group) ?
                string.Empty :
                (await groupDal.GetGroup(request.Group))?.Name;

            var triggerDomain = Resolve<TriggerDomain>();
            var putDataRequest = new JobOrTriggerDataRequest
            {
                DataKey = SummaryReportJob.EnableTriggerDataKey,
                DataValue = request.Enable.ToString(),
                Id = triggerId
            };
            await triggerDomain.PutData(putDataRequest, JobDomain.PutMode.Update, skipSystemCheck: true);

            putDataRequest = new JobOrTriggerDataRequest
            {
                DataKey = SummaryReportJob.GroupTriggerDataKey,
                DataValue = groupName,
                Id = triggerId
            };
            await triggerDomain.PutData(putDataRequest, JobDomain.PutMode.Update, skipSystemCheck: true);

            if (request.Enable)
            {
                await ResumeEnabledTrigger(scheduler, trigger);
            }
            else
            {
                await PauseDisabledTrigger(scheduler, trigger);
            }
        }

        private static async Task PauseDisabledTrigger(IScheduler scheduler, ITrigger trigger)
        {
            var state = await scheduler.GetTriggerState(trigger.Key);
            if (state == TriggerState.Paused) { return; }

            MonitorUtil.Lock(trigger.Key, 5, MonitorEvents.TriggerPaused);
            await scheduler.PauseTrigger(trigger.Key);
        }

        private static async Task ResumeEnabledTrigger(IScheduler scheduler, ITrigger trigger)
        {
            var state = await scheduler.GetTriggerState(trigger.Key);
            if (state != TriggerState.Paused) { return; }

            MonitorUtil.Lock(trigger.Key, 5, MonitorEvents.TriggerResumed);
            await scheduler.ResumeTrigger(trigger.Key);
        }

        private async Task ValidateGroupAndEmails(string groupName)
        {
            // validate group exists
            var groupDal = Resolve<GroupData>();
            var id = await groupDal.GetGroupId(groupName);
            var group =
                await groupDal.GetGroupWithUsers(id)
                ?? throw new RestValidationException("group", $"group with name '{groupName}' is not exists");

            // get all emails & validate
            var emails1 = group.Users.Select(u => u.EmailAddress1);
            var emails2 = group.Users.Select(u => u.EmailAddress1);
            var emails3 = group.Users.Select(u => u.EmailAddress1);

            var allEmails = emails1.Concat(emails2).Concat(emails3)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct();

            if (!allEmails.Any())
            {
                throw new RestValidationException("group", $"group with name '{groupName}' has no users with valid emails");
            }
        }
    }
}