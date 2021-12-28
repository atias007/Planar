namespace Planner.Service.General.Password
{
    internal class PasswordGeneratorProperties : IPasswordGeneratorProperties
    {
        public bool IncludeLowercase { get; set; }

        public bool IncludeUppercase { get; set; }

        public bool IncludeNumeric { get; set; }

        public bool IncludeSpecial { get; set; }

        public bool IncludeSpaces { get; set; }

        public int Length { get; set; } = 8;
    }
}