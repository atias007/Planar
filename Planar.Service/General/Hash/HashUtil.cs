using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Planar.Service.General.Hash
{
    internal static class HashUtil
    {
        public static HashEntity CreateHash(string value)
        {
            using var hmac = new HMACSHA512();
            var hashBytes = Encoding.UTF8.GetBytes(value);
            var result = new HashEntity
            {
                Value = value,
                Salt = hmac.Key,
                Hash = hmac.ComputeHash(hashBytes)
            };
            return result;
        }

        public static bool VerifyHash(string value, byte[] hash, byte[] salt)
        {
            using var hmac = new HMACSHA512(salt);
            var hashBytes = Encoding.UTF8.GetBytes(value);
            var computedHash = hmac.ComputeHash(hashBytes);
            var result = computedHash.SequenceEqual(hash);
            return result;
        }
    }
}