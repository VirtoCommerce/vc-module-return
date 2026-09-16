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
    private readonly IReturnStateProvider _stateProvider;

    public ReturnFlowService(
        ICustomerOrderService orderService,
        IReturnService returnService,
        IReturnEligibilityService eligibilityService,
        IReturnAttachmentService attachmentService,
        IStoreService storeService,
        IReturnStateProvider stateProvider)
    {
        _orderService = orderService;
        _returnService = returnService;
        _eligibilityService = eligibilityService;
        _attachmentService = attachmentService;
        _storeService = storeService;
        _stateProvider = stateProvider;
    }

    public virtual async Task<Return> CreateDraftAsync(CreateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var order = await _orderService.GetNoCloneAsync(request.OrderId)
            ?? throw new ReturnFlowException(ReturnFlowError.OrderNotFound, $"Order '{request.OrderId}' was not found.");

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

        if (!request.Items.Any(x => x.AttachmentUrls != null))
        {
            return result;
        }

        // Read back rather than reuse the assembled instance: saving resolves primary keys into the
        // models but leaves foreign keys like ReturnLineItem.ReturnId empty, and the update path
        // would patch those over the persisted rows.
        var saved = await _returnService.GetByIdAsync(result.Id);

        await UpdateAttachmentsAsync(saved, request.Items);
        await _returnService.SaveChangesAsync([saved]);

        return saved;
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

        orderReturn.Status = _stateProvider.GetNextStatus(ReturnAction.Submit, orderReturn.Status);

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

        if (!_stateProvider.IsAllowed(ReturnAction.Cancel, orderReturn.Status))
        {
            throw new ReturnFlowException(
                ReturnFlowError.WrongStatus,
                $"Return '{returnId}' is '{orderReturn.Status}' and can no longer be cancelled.");
        }

        orderReturn.Status = _stateProvider.GetNextStatus(ReturnAction.Cancel, orderReturn.Status);
        orderReturn.CancelReason = reason;

        await _returnService.SaveChangesAsync([orderReturn]);

        return orderReturn;
    }

    public virtual IList<ReturnFlowAction> GetAvailableActions(Return orderReturn)
    {
        return _stateProvider.GetActions(orderReturn);
    }

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

            if (!returnableItem.IsReturnable)
            {
                throw new ReturnFlowException(
                    ReturnFlowError.LineNotReturnable,
                    $"Line item '{lineItem.OrderLineItemId}' cannot be returned: {returnableItem.IneligibilityReason}.");
            }

            if (lineItem.Quantity < 1 || lineItem.Quantity > returnableItem.ReturnableQuantity)
            {
                throw new ReturnFlowException(
                    ReturnFlowError.QuantityUnavailable,
                    $"Line item '{lineItem.OrderLineItemId}': asked for {lineItem.Quantity}, {returnableItem.ReturnableQuantity} available.");
            }
        }
    }

    protected virtual async Task<Return> GetEditableDraftAsync(string returnId, ReturnFlowContext context)
    {
        var orderReturn = await GetOwnedReturnAsync(returnId, context);

        if (!_stateProvider.IsAllowed(ReturnAction.Edit, orderReturn.Status))
        {
            throw new ReturnFlowException(ReturnFlowError.WrongStatus, $"Return '{returnId}' is '{orderReturn.Status}', only a draft can be changed.");
        }

        return orderReturn;
    }

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

        var order = await _orderService.GetNoCloneAsync(orderReturn.OrderId);

        return order != null && order.CustomerId.EqualsIgnoreCase(customerId);
    }

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
