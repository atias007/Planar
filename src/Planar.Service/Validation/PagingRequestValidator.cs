using FluentValidation;
using Planar.API.Common.Entities;

namespace Planar.Service.Validation
{
    public class PagingRequestValidator : AbstractValidator<PagingRequest>
    {
        public PagingRequestValidator()
        {
            RuleFor(e => e.PageNumber).GreaterThan(0);
            RuleFor(e => e.PageSize).GreaterThan(0);
        }
    }
}