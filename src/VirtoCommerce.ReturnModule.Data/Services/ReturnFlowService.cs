using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnFlowService : IReturnFlowService
{
    private readonly ICustomerOrderService _orderService;
    private readonly IReturnService _returnService;
    private readonly IReturnEligibilityService _eligibilityService;
    private readonly IReturnAttachmentService _attachmentService;
    private readonly IStoreService _storeService;

    public ReturnFlowService(
        ICustomerOrderService orderService,
        IReturnService returnService,
        IReturnEligibilityService eligibilityService,
        IReturnAttachmentService attachmentService,
        IStoreService storeService)
    {
        _orderService = orderService;
        _returnService = returnService;
        _eligibilityService = eligibilityService;
        _attachmentService = attachmentService;
        _storeService = storeService;
    }

    public virtual async Task<Return> CreateDraftAsync(CreateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var order = await _orderService.GetNoCloneAsync(request.OrderId)
            ?? throw new ReturnFlowException(ReturnFlowError.OrderNotFound, $"Order '{request.OrderId}' was not found.");

        // The caller must own the order. Authorization already checked access, but a draft must not
        // end up owned by somebody other than the buyer whose order it is.
        if (!order.CustomerId.EqualsIgnoreCase(context.CustomerId))
        {
            throw new ReturnFlowException(ReturnFlowError.OrderNotFound, $"Order '{request.OrderId}' was not found.");
        }

        var eligibility = await _eligibilityService.GetOrderEligibilityAsync(order);

        if (!eligibility.IsEligible)
        {
            throw new ReturnFlowException(ReturnFlowError.OrderNotEligible, eligibility.Reason);
        }

        if (request.Items.IsNullOrEmpty())
        {
            throw new ReturnFlowException(ReturnFlowError.NoItems, "A return needs at least one line.");
        }

        var orderLineItems = (order.Items ?? []).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var result = AbstractTypeFactory<Return>.TryCreateInstance();
        result.Status = ReturnStatus.Draft;
        result.OrderId = order.Id;
        result.OrderNumber = order.Number;
        result.StoreId = order.StoreId;
        result.CustomerId = order.CustomerId;
        result.CustomerName = order.CustomerName ?? context.CustomerName;
        result.CustomerReference = request.CustomerReference ?? order.PurchaseOrderNumber;
        result.CustomerComment = request.CustomerComment;
        result.LineItems = request.Items.Select(x => CreateLineItem(x, orderLineItems)).ToList();

        await _returnService.SaveChangesAsync([result]);

        // After the first save: the files are owned by the return, which has no id before it.
        await UpdateAttachmentsAsync(result, request.Items);

        return result;
    }

    public virtual async Task<Return> UpdateDraftAsync(UpdateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var orderReturn = await GetEditableDraftAsync(request.ReturnId, context);
        var order = await _orderService.GetNoCloneAsync(orderReturn.OrderId);
        var orderLineItems = (order?.Items ?? []).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        orderReturn.CustomerReference = request.CustomerReference ?? orderReturn.CustomerReference;
        orderReturn.CustomerComment = request.CustomerComment ?? orderReturn.CustomerComment;

        if (request.Items != null)
        {
            if (request.Items.Count == 0)
            {
                throw new ReturnFlowException(ReturnFlowError.NoItems, "A return needs at least one line.");
            }

            orderReturn.LineItems = request.Items.Select(x => UpdateLineItem(x, orderReturn, orderLineItems)).ToList();

            await UpdateAttachmentsAsync(orderReturn, request.Items);
        }

        await _returnService.SaveChangesAsync([orderReturn]);

        return orderReturn;
    }

    /// <summary>
    /// Applies each line's attachment list, skipping lines the caller said nothing about.
    /// </summary>
    protected virtual async Task UpdateAttachmentsAsync(Return orderReturn, IList<CreateReturnItemRequest> items)
    {
        foreach (var item in items.Where(x => x.AttachmentUrls != null))
        {
            var lineItem = orderReturn.LineItems
                .FirstOrDefault(x => x.OrderLineItemId.EqualsIgnoreCase(item.OrderLineItemId));

            if (lineItem != null)
            {
                await _attachmentService.UpdateAttachmentsAsync(orderReturn, lineItem, item.AttachmentUrls);
            }
        }
    }

    public virtual async Task<Return> SubmitAsync(string returnId, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderReturn = await GetEditableDraftAsync(returnId, context);

        if (orderReturn.LineItems.IsNullOrEmpty())
        {
            throw new ReturnFlowException(ReturnFlowError.NoItems, "A return needs at least one line.");
        }

        var order = await _orderService.GetNoCloneAsync(orderReturn.OrderId)
            ?? throw new ReturnFlowException(ReturnFlowError.OrderNotFound, $"Order '{orderReturn.OrderId}' was not found.");

        await ValidateAttachmentsAsync(orderReturn);
        await ValidateAvailabilityAsync(orderReturn, order);

        orderReturn.Status = ReturnStatus.Requested;

        foreach (var lineItem in orderReturn.LineItems)
        {
            lineItem.ItemState ??= ReturnItemState.Requested;
        }

        await _returnService.SaveChangesAsync([orderReturn]);

        return orderReturn;
    }

    public virtual async Task<Return> CancelAsync(string returnId, string reason, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderReturn = await GetOwnedReturnAsync(returnId, context);

        if (!CancellableStatuses.Contains(orderReturn.Status ?? string.Empty))
        {
            throw new ReturnFlowException(
                ReturnFlowError.WrongStatus,
                $"Return '{returnId}' is '{orderReturn.Status}' and can no longer be cancelled.");
        }

        orderReturn.Status = ReturnStatus.Cancelled;
        orderReturn.CancelReason = reason;

        await _returnService.SaveChangesAsync([orderReturn]);

        return orderReturn;
    }

    /// <summary>
    /// States a buyer may still withdraw from.
    /// </summary>
    /// <remarks>
    /// Requested is included per the spec's transition table. It does let a buyer pull a return out
    /// from under an agent who is already looking at it — there is no agent side yet, so nothing can
    /// collide today, and a store that later wants a stricter rule narrows this to Draft alone.
    /// </remarks>
    protected virtual ISet<string> CancellableStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ReturnStatus.Draft, ReturnStatus.Requested };

    /// <summary>
    /// Every returned line must carry at least one photo or document, when the store asks for it.
    /// </summary>
    /// <remarks>
    /// Per line, not per return: a claim about one product is not evidence about another. Whether
    /// it is demanded at all is the store's call via Return.AttachmentsRequired.
    /// </remarks>
    protected virtual async Task ValidateAttachmentsAsync(Return orderReturn)
    {
        var store = string.IsNullOrEmpty(orderReturn.StoreId) ? null : await _storeService.GetNoCloneAsync(orderReturn.StoreId);
        var settings = store?.Settings ?? [];

        if (!settings.GetValue<bool>(ModuleConstants.Settings.General.ReturnAttachmentsRequired))
        {
            return;
        }

        var lineWithoutFiles = orderReturn.LineItems.FirstOrDefault(x => x.Attachments.IsNullOrEmpty());

        if (lineWithoutFiles != null)
        {
            throw new ReturnFlowException(
                ReturnFlowError.AttachmentsRequired,
                $"Line item '{lineWithoutFiles.OrderLineItemId}' needs at least one photo or document.");
        }
    }

    /// <summary>
    /// Re-checks every line against what is returnable right now.
    /// </summary>
    /// <remarks>
    /// The draft itself is excluded, otherwise it would compete with its own quantities. Between
    /// drafting and submitting somebody else in the organization may have claimed the same units,
    /// which is exactly why this runs here rather than at CreateDraft.
    /// </remarks>
    protected virtual async Task ValidateAvailabilityAsync(Return orderReturn, CustomerOrder order)
    {
        var returnableItems = (await _eligibilityService.GetReturnableItemsAsync(order, orderReturn.Id))
            .ToDictionary(x => x.OrderLineItemId, StringComparer.OrdinalIgnoreCase);

        foreach (var lineItem in orderReturn.LineItems)
        {
            if (!returnableItems.TryGetValue(lineItem.OrderLineItemId ?? string.Empty, out var returnableItem))
            {
                throw new ReturnFlowException(ReturnFlowError.LineItemNotFound, $"Line item '{lineItem.OrderLineItemId}' is not on this order.");
            }

            if (lineItem.Quantity < 1 || lineItem.Quantity > returnableItem.ReturnableQuantity)
            {
                throw new ReturnFlowException(
                    ReturnFlowError.QuantityUnavailable,
                    $"Line item '{lineItem.OrderLineItemId}': asked for {lineItem.Quantity}, {returnableItem.ReturnableQuantity} available.");
            }
        }
    }

    /// <summary>
    /// Loads a draft the caller is allowed to edit.
    /// </summary>
    /// <remarks>
    /// A return that is not the caller's reads as missing rather than forbidden, so probing ids
    /// tells an outsider nothing.
    /// </remarks>
    protected virtual async Task<Return> GetEditableDraftAsync(string returnId, ReturnFlowContext context)
    {
        var orderReturn = await GetOwnedReturnAsync(returnId, context);

        if (!ReturnStatus.Draft.EqualsIgnoreCase(orderReturn.Status))
        {
            throw new ReturnFlowException(ReturnFlowError.WrongStatus, $"Return '{returnId}' is '{orderReturn.Status}', only a draft can be changed.");
        }

        return orderReturn;
    }

    /// <summary>
    /// Loads a return belonging to the caller, whatever state it is in.
    /// </summary>
    protected virtual async Task<Return> GetOwnedReturnAsync(string returnId, ReturnFlowContext context)
    {
        var orderReturn = await _returnService.GetByIdAsync(returnId, ReturnResponseGroup.None.ToString());

        if (orderReturn == null || !await IsOwnedByAsync(orderReturn, context.CustomerId))
        {
            throw new ReturnFlowException(ReturnFlowError.ReturnNotFound, $"Return '{returnId}' was not found.");
        }

        return orderReturn;
    }

    protected virtual async Task<bool> IsOwnedByAsync(Return orderReturn, string customerId)
    {
        if (!string.IsNullOrEmpty(orderReturn.CustomerId))
        {
            return orderReturn.CustomerId.EqualsIgnoreCase(customerId);
        }

        // Returns raised in the admin UI before the field existed fall back to their order.
        var order = await _orderService.GetNoCloneAsync(orderReturn.OrderId);

        return order != null && order.CustomerId.EqualsIgnoreCase(customerId);
    }

    /// <summary>
    /// Reuses the existing line when the buyer still wants that order line, so its id and audit
    /// trail survive an edit instead of the draft being rebuilt from scratch each time.
    /// </summary>
    protected virtual ReturnLineItem UpdateLineItem(CreateReturnItemRequest request, Return orderReturn, IDictionary<string, LineItem> orderLineItems)
    {
        var existing = orderReturn.LineItems?
            .FirstOrDefault(x => x.OrderLineItemId.EqualsIgnoreCase(request.OrderLineItemId));

        var result = existing ?? CreateLineItem(request, orderLineItems);

        if (request.Quantity < 1)
        {
            throw new ReturnFlowException(ReturnFlowError.InvalidQuantity, $"Line item '{request.OrderLineItemId}' needs a quantity of at least one.");
        }

        result.Quantity = request.Quantity;
        result.ReasonCode = request.ReasonCode ?? result.ReasonCode;
        result.ReasonComment = request.ReasonComment ?? result.ReasonComment;
        result.SerialNumber = request.SerialNumber ?? result.SerialNumber;

        return result;
    }

    protected virtual ReturnLineItem CreateLineItem(CreateReturnItemRequest request, IDictionary<string, LineItem> orderLineItems)
    {
        if (!orderLineItems.TryGetValue(request.OrderLineItemId ?? string.Empty, out var orderLineItem))
        {
            throw new ReturnFlowException(ReturnFlowError.LineItemNotFound, $"Line item '{request.OrderLineItemId}' is not on this order.");
        }

        if (request.Quantity < 1)
        {
            throw new ReturnFlowException(ReturnFlowError.InvalidQuantity, $"Line item '{request.OrderLineItemId}' needs a quantity of at least one.");
        }

        var result = AbstractTypeFactory<ReturnLineItem>.TryCreateInstance();

        result.OrderLineItemId = orderLineItem.Id;
        result.Quantity = request.Quantity;
        result.ReasonCode = request.ReasonCode;
        result.ReasonComment = request.ReasonComment;
        result.SerialNumber = request.SerialNumber;
        result.ItemState = ReturnItemState.Requested;

        // Snapshots: the return has to stay readable even after the catalog moves on, and the
        // negotiated price must not drift.
        result.ProductId = orderLineItem.ProductId;
        result.Sku = orderLineItem.Sku;
        result.Name = orderLineItem.Name;
        result.ImageUrl = orderLineItem.ImageUrl;
        result.MeasureUnit = orderLineItem.MeasureUnit;
        result.OrderedQuantity = orderLineItem.Quantity;
        result.Price = orderLineItem.Price;

        return result;
    }
}
