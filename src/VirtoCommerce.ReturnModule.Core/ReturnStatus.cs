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

    public const string AwaitingDelivery = "AwaitingDelivery";
    public const string Received = "Received";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
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
