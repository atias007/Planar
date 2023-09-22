namespace Planar.API.Common.Entities
{
    public class AppSettingsInfo
    {
        public GeneralSettingsInfo General { get; set; } = null!;

        public static SmtpSettingsInfo Smtp { get; set; } = null!;

        public static AuthenticationSettingsInfo Authentication { get; set; } = null!;

        public static ClusterSettingsInfo Cluster { get; set; } = null!;

        public static RetentionSettingsInfo Retention { get; set; } = null!;

        public static DatabaseSettingsInfo Database { get; set; } = null!;
    }
}