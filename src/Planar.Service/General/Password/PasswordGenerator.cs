using Planar.Common.Exceptions;
using Planar.Service.Exceptions;
using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Planar.Service.General.Password
{
    public static class PasswordGenerator
    {
        /// <summary>
        /// Generates a random password based on the rules passed in the parameters
        /// </summary>
        /// <param name="IncludeLowercase">Bool to say if lowercase are required</param>
        /// <param name="IncludeUppercase">Bool to say if uppercase are required</param>
        /// <param name="IncludeNumeric">Bool to say if numerics are required</param>
        /// <param name="IncludeSpecial">Bool to say if special characters are required</param>
        /// <param name="IncludeSpaces">Bool to say if spaces are required</param>
        /// <param name="Length">Length of password required. Should be between 8 and 128</param>
        /// <returns></returns>
        public static string GeneratePassword(IPasswordGeneratorProperties properties)
        {
            const int MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS = 2;
            const string LOWERCASE_CHARACTERS = "abcdefghijklmnopqrstuvwxyz";
            const string UPPERCASE_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string NUMERIC_CHARACTERS = "0123456789";
            const string SPECIAL_CHARACTERS = @"!#$%&*@\";
            const string SPACE_CHARACTER = " ";
            const int PASSWORD_LENGTH_MIN = 8;
            const int PASSWORD_LENGTH_MAX = 128;

            if (properties.Length < PASSWORD_LENGTH_MIN || properties.Length > PASSWORD_LENGTH_MAX)
            {
                throw new PlanarException("Password length must be between 8 and 128.");
            }

            string characterSet = "";

            if (properties.IncludeLowercase)
            {
                characterSet += LOWERCASE_CHARACTERS;
            }

            if (properties.IncludeUppercase)
            {
                characterSet += UPPERCASE_CHARACTERS;
            }

            if (properties.IncludeNumeric)
            {
                characterSet += NUMERIC_CHARACTERS;
            }

            if (properties.IncludeSpecial)
            {
                characterSet += SPECIAL_CHARACTERS;
            }

            if (properties.IncludeSpaces)
            {
                characterSet += SPACE_CHARACTER;
            }

            var password = new char[properties.Length];
            var characterSetLength = characterSet.Length;

            for (int characterPosition = 0; characterPosition < properties.Length; characterPosition++)
            {
                password[characterPosition] = characterSet[RandomNumberGenerator.GetInt32(characterSetLength - 1)];

                bool moreThanTwoIdenticalInARow =
                    characterPosition > MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS
                    && password[characterPosition] == password[characterPosition - 1]
                    && password[characterPosition - 1] == password[characterPosition - 2];

                if (moreThanTwoIdenticalInARow)
                {
                    characterPosition--;
                }
            }

            return string.Join(null, password);
        }

        /// <summary>
        /// Checks if the password created is valid
        /// </summary>
        /// <param name="includeLowercase">Bool to say if lowercase are required</param>
        /// <param name="includeUppercase">Bool to say if uppercase are required</param>
        /// <param name="includeNumeric">Bool to say if numerics are required</param>
        /// <param name="includeSpecial">Bool to say if special characters are required</param>
        /// <param name="includeSpaces">Bool to say if spaces are required</param>
        /// <param name="password">Generated password</param>
        /// <returns>True or False to say if the password is valid or not</returns>
        public static bool PasswordIsValid(IPasswordGeneratorProperties properties, string password)
        {
            const string REGEX_LOWERCASE = @"[a-z]";
            const string REGEX_UPPERCASE = @"[A-Z]";
            const string REGEX_NUMERIC = @"[\d]";
            const string REGEX_SPECIAL = @"([!#$%&*@\\])+";
            const string REGEX_SPACE = @"([ ])+";

            bool lowerCaseIsValid = !properties.IncludeLowercase || (properties.IncludeLowercase && Regex.IsMatch(password, REGEX_LOWERCASE));
            bool upperCaseIsValid = !properties.IncludeUppercase || (properties.IncludeUppercase && Regex.IsMatch(password, REGEX_UPPERCASE));
            bool numericIsValid = !properties.IncludeNumeric || (properties.IncludeNumeric && Regex.IsMatch(password, REGEX_NUMERIC));
            bool symbolsAreValid = !properties.IncludeSpecial || (properties.IncludeSpecial && Regex.IsMatch(password, REGEX_SPECIAL));
            bool spacesAreValid = !properties.IncludeSpaces || (properties.IncludeSpaces && Regex.IsMatch(password, REGEX_SPACE));

            return lowerCaseIsValid && upperCaseIsValid && numericIsValid && symbolsAreValid && spacesAreValid;
        }
    }
}