namespace VirtoCommerce.ReturnModule.Data.Handlers;

/// <summary>
/// Serialised into the Hangfire job store, so its name and namespace are part of the persisted
/// payload: renaming or moving it breaks the jobs in flight at deploy time.
/// <para>
/// It carries only what identifies the job. The recipient and the content come from the return as
/// it is when the job runs, so the two cannot disagree.
/// </para>
/// </summary>
public class ReturnNotificationJobArgument
{
    public string ReturnId { get; set; }

    public string StoreId { get; set; }

    public string NotificationTypeName { get; set; }
}
