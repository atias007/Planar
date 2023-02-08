using NUnit.Framework;
using Planar.Service.General.Password;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Planar.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestPassword()
        {
            var password = PasswordGenerator.GeneratePassword(
               new PasswordGeneratorBuilder()
               .IncludeLowercase()
               .IncludeNumeric()
               .IncludeSpecial()
               .IncludeUppercase()
               .WithLength(12)
               .Build());

            using (var hmac = new HMACSHA512())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(passwordBytes);
            }

            Assert.Pass();
        }
    }
}