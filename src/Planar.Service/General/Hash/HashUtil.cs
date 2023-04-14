using Microsoft.IdentityModel.Tokens;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
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

        public static string CreateToken(UserIdentity user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.RoleId.ToString()),
                new Claim(ClaimTypes.Name, user.Username )
            };

            const string secret = "DWPVy9Xefs7JnI4mMbZMrPhp39QWpDIO";
            var tokenExpireSpan = TimeSpan.FromMinutes(20);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.Add(tokenExpireSpan),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var result = tokenHandler.WriteToken(token);
            return result;
        }
    }
}