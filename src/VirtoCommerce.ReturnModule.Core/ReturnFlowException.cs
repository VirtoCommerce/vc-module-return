using System;

namespace VirtoCommerce.ReturnModule.Core;

/// <summary>
/// Machine-readable failures of a return transition. Surfaced to GraphQL as an error code so the
/// storefront can localize it and keep the buyer's draft instead of losing it.
/// </summary>
public static class ReturnFlowError
{
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderNotEligible = "ORDER_NOT_ELIGIBLE";
    public const string LineItemNotFound = "LINE_ITEM_NOT_FOUND";
    public const string InvalidQuantity = "INVALID_QUANTITY";
    public const string NoItems = "NO_ITEMS";
    public const string ReturnNotFound = "RETURN_NOT_FOUND";
    public const string WrongStatus = "WRONG_STATUS";
    public const string QuantityUnavailable = "RETURN_QUANTITY_UNAVAILABLE";
    public const string AttachmentsRequired = "ATTACHMENTS_REQUIRED";
}

public class ReturnFlowException : Exception
{
    public ReturnFlowException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
