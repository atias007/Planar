using Quartz;
using System.Linq;

namespace Planar.Common.Validation;

public static class ValidationUtil
{
    public static bool IsValidNodeInstanceId(string? value)
    {
        return !string.IsNullOrEmpty(value)
            && value.Length <= 50
            && value.Length >= 3
            && value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    public static bool IsValidCronExpression(string? expression)
    {
        if (expression == null) { return true; }
        if (string.IsNullOrWhiteSpace(expression)) { return false; }
        try
        {
            CronExpression.ValidateExpression(expression);
            return true;
        }
        catch
        {
            return false;
        }
    }
}