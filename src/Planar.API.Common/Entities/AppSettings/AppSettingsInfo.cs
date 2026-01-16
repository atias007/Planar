namespace Planar.API.Common.Entities
{
    public class AppSettingsInfo
    {
        public AuthenticationSettingsInfo Authentication { get; set; } = null!;
        public ClusterSettingsInfo Cluster { get; set; } = null!;
        public DatabaseSettingsInfo Database { get; set; } = null!;
        public GeneralSettingsInfo General { get; set; } = null!;
        public RetentionSettingsInfo Retention { get; set; } = null!;
        public SmtpSettingsInfo Smtp { get; set; } = null!;
        public MonitorSettingsInfo Monitor { get; set; } = null!;
        public ProtectionSettingsInfo Protection { get; set; } = null!;
        public CentralConfigInfo CentralConfig { get; set; } = null!;
        public HooksSettingsInfo Hooks { get; } = new HooksSettingsInfo();
    }
}