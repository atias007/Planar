namespace Planar.Service;

internal static class ErrorCodes
{
    public static class General
    {
        public const string ContentTypeNotSupported = "GN001";
    }

    public static class User
    {
        public const string UsernameNotExists = "U0001";
        public const string WrongPassword = "U0002";
    }

    public static class Job
    {
        public const string MaxJobDataItems = "J0001";
        public const string JobDataKeyEmpty = "J0002";
        public const string JobDataKeyInvalid = "J0003";
        public const string YamlFileInvalid = "J0004";
        public const string JobTypeNotSupported = "J0005";
    }

    public static class Trigger
    {
        public const string InvalidInterval = "T0001";
    }
}