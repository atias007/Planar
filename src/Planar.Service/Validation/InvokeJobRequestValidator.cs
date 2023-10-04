using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class InvokeJobRequestValidator : AbstractValidator<InvokeJobRequest>
    {
        public InvokeJobRequestValidator()
        {
            Include(new JobOrTriggerKeyValidator());
            RuleFor(x => x.NowOverrideValue).ValidSqlDateTime();
        }
    }
}