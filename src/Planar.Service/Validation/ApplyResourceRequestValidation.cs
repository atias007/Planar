using FluentValidation;
using Planar.Service.Model;

namespace Planar.Service.Validation;

public class ApplyResourceRequestValidation : AbstractValidator<ApplyResourceRequest>
{
    public ApplyResourceRequestValidation()
    {
        Include(new ResourceModelValidation());
    }
}