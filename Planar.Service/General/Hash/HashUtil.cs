using System.Security.Cryptography;
using System.Text;

namespace Planar.Service.General.Hash
{
    internal static class HashUtil
    {
        public static HashEntity CreateHash(string value)
        {
            using var hmac = new HMACSHA512();
            var passwordBytes = Encoding.UTF8.GetBytes(value);
            var result = new HashEntity { Value = value };
            result.Salt = hmac.Key;
            result.Hash = hmac.ComputeHash(passwordBytes);
            return result;
        }
    }
}