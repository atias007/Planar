namespace Planar.Common
{
    public class HooksSettings
    {
        public RestHookSettings Rest { get; set; } = new();
        public TeamsHookSettings Teams { get; set; } = new();
        public TwilioSmsHookSettings TwilioSms { get; set; } = new();
    }

    public class RestHookSettings
    {
        public string? DefaultUrl { get; set; }
    }

    public class TeamsHookSettings
    {
        public string? DefaultUrl { get; set; }
        public bool SendToMultipleUrls { get; set; }
    }

    public class TwilioSmsHookSettings
    {
        public string? AccountSid { get; set; }
        public string? AuthToken { get; set; }
        public string? FromNumber { get; set; }
        public string? DefaultPhonePrefix { get; set; }
    }
}