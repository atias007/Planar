using Quartz;

namespace Planar.Common.Validation
{
    internal static class ValidationUtil
    {
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
}