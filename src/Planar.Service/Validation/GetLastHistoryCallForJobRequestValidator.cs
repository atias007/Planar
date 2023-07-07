using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class GetLastHistoryCallForJobRequestValidator : AbstractValidator<GetLastHistoryCallForJobRequest>
    {
        public GetLastHistoryCallForJobRequestValidator()
        {
            Include(new PagingRequestValidator());
            RuleFor(e => e.LastDays).GreaterThan(0);
        }
    }
}