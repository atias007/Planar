using FluentValidation;

namespace Planar.Service.Validation
{
    public class SqlConnectionStringValidator : AbstractValidator<SqlConnectionString>
    {
        public SqlConnectionStringValidator()
        {
            RuleFor(i => i.Name).NotEmpty().Length(1, 50);
            RuleFor(i => i.ConnectionString).NotEmpty().Length(5, 500);
        }
    }
}