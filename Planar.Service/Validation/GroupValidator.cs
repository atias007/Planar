using FluentValidation;
using Planar.Service.Model;

namespace Planar.Service.Validation
{
    public class GroupValidator : AbstractValidator<Group>
    {
        public GroupValidator()
        {
            RuleFor(g => g.Name).NotEmpty().Length(2, 50);
            RuleFor(u => u.Reference1).MaximumLength(500);
            RuleFor(u => u.Reference2).MaximumLength(500);
            RuleFor(u => u.Reference3).MaximumLength(500);
            RuleFor(u => u.Reference4).MaximumLength(500);
            RuleFor(u => u.Reference5).MaximumLength(500);
        }
    }
}