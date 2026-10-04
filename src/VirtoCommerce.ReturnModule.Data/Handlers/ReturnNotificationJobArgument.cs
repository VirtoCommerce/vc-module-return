namespace VirtoCommerce.ReturnModule.Data.Handlers;

// The payload of the return notification jobs, serialised into the job store (and into Hangfire jobs queued by
// earlier versions): renaming or moving it breaks the jobs in flight at deploy time.
// It identifies the job only; the recipient and the content come from the return as it is when the job runs.
public class ReturnNotificationJobArgument
{
    public string ReturnId { get; set; }

    public string StoreId { get; set; }

    public string NotificationTypeName { get; set; }
}
