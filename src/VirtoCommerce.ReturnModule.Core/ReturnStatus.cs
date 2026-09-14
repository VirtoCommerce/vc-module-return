namespace VirtoCommerce.ReturnModule.Core;

/// <summary>
/// Return statuses. Declared in code rather than taken from the editable Return.Status dictionary,
/// because the transition rules key off them; the dictionary stays for display names only.
/// </summary>
public static class ReturnStatus
{
    public const string Draft = "Draft";
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string PartiallyApproved = "PartiallyApproved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    /// <summary>
    /// Declared but not reachable yet, so later iterations add transitions instead of renaming states.
    /// </summary>
    public const string AwaitingDelivery = "AwaitingDelivery";
    public const string Received = "Received";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
}

/// <summary>
/// Per-line states. Only <see cref="Requested"/> is reachable while approving lives in a later step.
/// </summary>
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
