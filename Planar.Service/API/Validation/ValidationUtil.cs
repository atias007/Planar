using System.Text.RegularExpressions;

namespace Planar.Service.API.Validation
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

        public static bool IsValidNumeric(string value)
        {
            if (value == null) return true;
            const string pattern = "^[0-9]+$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(value);
        }
    }
}