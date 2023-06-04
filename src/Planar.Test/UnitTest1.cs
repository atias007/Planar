using NUnit.Framework;
using Planar.Service.General.Password;
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
            //// var x = Normal.InvCDF(0, 1, 0.975);

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

                Assert.That(salt, Is.Not.Null);
                Assert.That(hash, Is.Not.Null);
            }
        }
    }
}