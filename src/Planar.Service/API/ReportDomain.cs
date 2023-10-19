using Planar.Api.Common.Entities;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Linq;
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
            // validate group exists
            var groupDal = Resolve<GroupData>();
            var id = await groupDal.GetGroupId(request.Group!);
            var group =
                await groupDal.GetGroupWithUsers(id)
                ?? throw new RestConflictException($"group with name '{request.Group}' is not exists");

            // get all emails & validate
            var emails1 = group.Users.Select(u => u.EmailAddress1);
            var emails2 = group.Users.Select(u => u.EmailAddress1);
            var emails3 = group.Users.Select(u => u.EmailAddress1);

            var allEmails = emails1.Concat(emails2).Concat(emails3)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct();

            if (!allEmails.Any())
            {
                throw new RestConflictException($"group with name '{request.Group}' has no users with valid emails");
            }

            // set default period
            if (string.IsNullOrEmpty(request.Period))
            {
                request.Period = SummaryReportPeriods.Daily.ToString();
            }
        }
    }
}