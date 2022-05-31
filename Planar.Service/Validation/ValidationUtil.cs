using Planar.Service.API.Helpers;
using System.Text.RegularExpressions;

namespace Planar.Service.Validation
{
    public static class ValidationUtil
    {
        public static bool IsValidEmail(string value)
        {
            if (value == null) return true;
            const string pattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9]{2,8}(?:[a-z0-9-]*[a-z0-9])?)\Z";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(value);
        }

        public static bool IsOnlyDigits(string value)
        {
            if (value == null) return true;
            const string pattern = "^[0-9]+$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(value);
        }

        public static bool IsJobExists(string value)
        {
            if (value == null) return true;
            var key = JobKeyHelper.GetJobKeyById(value).Result;
            return key != null;
        }
    }
}