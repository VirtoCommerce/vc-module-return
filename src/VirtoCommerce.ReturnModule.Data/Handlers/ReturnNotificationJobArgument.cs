namespace VirtoCommerce.ReturnModule.Data.Handlers;

// Serialised into the Hangfire job store: renaming or moving it breaks the jobs in flight at deploy time.
// It identifies the job only; the recipient and the content come from the return as it is when the job runs.
public class ReturnNotificationJobArgument
{
    public string ReturnId { get; set; }

    public string StoreId { get; set; }

    public string NotificationTypeName { get; set; }
}
