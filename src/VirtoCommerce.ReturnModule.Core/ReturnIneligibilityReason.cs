namespace VirtoCommerce.ReturnModule.Core;

/// <summary>
/// Machine-readable reasons a line cannot be returned. The storefront localizes these —
/// never show them to a buyer as-is.
/// </summary>
public static class ReturnIneligibilityReason
{
    public const string OrderStatusNotAllowed = "ORDER_STATUS_NOT_ALLOWED";
    public const string LineCancelled = "LINE_CANCELLED";
    public const string NotDelivered = "NOT_DELIVERED";
    public const string OutsideReturnWindow = "OUTSIDE_RETURN_WINDOW";
    public const string NothingLeftToReturn = "NOTHING_LEFT_TO_RETURN";
}
