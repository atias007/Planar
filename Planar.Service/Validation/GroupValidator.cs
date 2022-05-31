using FluentValidation;
using Planar.Service.Model;

namespace Planar.Service.Validation
{
    public class GroupValidator : AbstractValidator<Group>
    {
        public GroupValidator()
        {
            RuleFor(g => g.Name).NotEmpty().Length(2, 50);
            RuleFor(u => u.Reference1).Length(0, 500);
            RuleFor(u => u.Reference2).Length(0, 500);
            RuleFor(u => u.Reference3).Length(0, 500);
            RuleFor(u => u.Reference4).Length(0, 500);
            RuleFor(u => u.Reference5).Length(0, 500);
        }
    }
}