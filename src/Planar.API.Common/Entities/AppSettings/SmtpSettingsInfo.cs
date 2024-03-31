namespace Planar.API.Common.Entities
{
    public class SmtpSettingsInfo
    {
        public string? FromAddress { get; set; }
        public string? FromName { get; set; }
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? HtmlImageMode { get; set; }
        public string? HtmlImageInternalBaseUrl { get; set; }
    }
}