namespace SeqAlertsCheck;

internal class AlertState
{
    public required string AlertId { get; set; }
    public required string Title { get; set; }
    public required DateTime? SuppressedUntil { get; set; }
    public bool IsFailing { get; set; }
    public string? OwnerId { get; set; }

    ////public object[] NotificationAppInstanceIds { get; set; }
    ////public DateTime LastCheck { get; set; }
    ////public object LastNotification { get; set; }

    ////public string Id { get; set; }
    ////public Links Links { get; set; }
}

////public class Links
////{
////    public string Self { get; set; }
////    public string Group { get; set; }
////}