using System;
using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core;

public static class ReturnFlowError
{
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderNotEligible = "ORDER_NOT_ELIGIBLE";
    public const string LineItemNotFound = "LINE_ITEM_NOT_FOUND";
    public const string InvalidQuantity = "INVALID_QUANTITY";
    public const string NoItems = "NO_ITEMS";
    public const string DuplicateLine = "DUPLICATE_LINE";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string AttachmentNotAvailable = "ATTACHMENT_NOT_AVAILABLE";
    public const string ReturnNotFound = "RETURN_NOT_FOUND";
    public const string WrongStatus = "WRONG_STATUS";
    public const string QuantityUnavailable = "RETURN_QUANTITY_UNAVAILABLE";
    public const string AttachmentsRequired = "ATTACHMENTS_REQUIRED";
    public const string LineNotReturnable = "LINE_NOT_RETURNABLE";
}

public class ReturnFlowException : Exception
{
    public ReturnFlowException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    /// <summary>
    /// What a caller needs to act on the error, as values rather than as a sentence. The message
    /// says "asked for 6, 4 available" in English only, and a storefront with ten languages cannot
    /// translate that by parsing it. These reach the client as GraphQL error extensions.
    /// </summary>
    public IDictionary<string, object> Values { get; } = new Dictionary<string, object>();

    public ReturnFlowException WithValue(string name, object value)
    {
        Values[name] = value;

        return this;
    }
}

public static class ReturnFlowErrorValue
{
    public const string OrderLineItemId = "orderLineItemId";
    public const string LineItemId = "lineItemId";
    public const string RequestedQuantity = "requestedQuantity";
    public const string AvailableQuantity = "availableQuantity";
    public const string IneligibilityReason = "ineligibilityReason";
}
