using Microsoft.IdentityModel.Tokens;
using Planar.Common;
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
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (ClaimTypes.Role, user.Role.ToString()),
                new (ClaimTypes.Name, user.Username),
                new (ClaimTypes.Surname, user.Surename),
                new (ClaimTypes.GivenName, user.GivenName ?? string.Empty),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = AuthenticationSettings.AuthenticationIssuer,
                Audience = AuthenticationSettings.AuthenticationAudience,
                Expires = DateTime.UtcNow.Add(AppSettings.Authentication.TokenExpire),
                SigningCredentials = new SigningCredentials(AppSettings.Authentication.Key, SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(claims),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var result = tokenHandler.WriteToken(token);
            return result;
        }
    }
}