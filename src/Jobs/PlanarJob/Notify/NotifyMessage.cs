namespace PlanarJob.Notify;

public class NotifyMessage
{
    public int Progress { get; set; }
    public string JobId { get; set; } = null!;
    public string FireInstanceId { get; set; } = null!;
}