using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core;

public static class ReturnStatus
{
    public const string Draft = "Draft";
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string PartiallyApproved = "PartiallyApproved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    // What the module shipped with and what the admin still creates a return as.
    public const string New = "New";

    // The spelling the module shipped with. The Return.Status dictionary keeps both, so rows carry either.
    public const string LegacyCancelled = "Canceled";

    public const string AwaitingDelivery = "AwaitingDelivery";
    public const string Received = "Received";
    public const string Processing = "Processing";
    public const string Completed = "Completed";

    public static string Normalize(string status)
    {
        return status.EqualsIgnoreCase(LegacyCancelled) ? Cancelled : status ?? string.Empty;
    }
}

public static class ReturnItemState
{
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public const string Received = "Received";
    public const string Inspected = "Inspected";
    public const string Resolved = "Resolved";
    public const string Missing = "Missing";
}
