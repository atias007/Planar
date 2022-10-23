namespace Planar.Service.General.Password
{
    public interface IPasswordGeneratorProperties
    {
        bool IncludeLowercase { get; }
        bool IncludeUppercase { get; }
        bool IncludeNumeric { get; }
        bool IncludeSpecial { get; }
        bool IncludeSpaces { get; }
        int Length { get; }
    }
}