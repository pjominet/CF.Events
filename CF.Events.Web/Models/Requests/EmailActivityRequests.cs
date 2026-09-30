namespace CF.Events.Web.Models.Requests;

public class EmailActivityFetchRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Hours { get; set; }
    public string? Search { get; set; }
    public string? SearchSender { get; set; }
    public string? SearchRecipient { get; set; }
    public string? SearchSubject { get; set; }
    public List<string>? EventTypes { get; set; }
}

public record EmailActivitySyncResult(
    int TotalEventsFetched,
    int EmailsProcessed,
    int NewEventsAdded,
    int UpdatedEmailsCount
);
