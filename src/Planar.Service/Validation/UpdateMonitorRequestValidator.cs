using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class UpdateMonitorRequestValidator : AbstractValidator<UpdateMonitorRequest>
    {
        public UpdateMonitorRequestValidator(IValidator<AddMonitorRequest> addValidator)
        {
            Include(addValidator);
            RuleFor(e => e.Id).GreaterThan(0);
        }
    }
}