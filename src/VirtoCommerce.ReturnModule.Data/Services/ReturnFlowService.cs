using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnFlowService : IReturnFlowService
{
    private readonly ICustomerOrderService _orderService;
    private readonly IReturnService _returnService;
    private readonly IReturnEligibilityService _eligibilityService;
    private readonly IReturnAttachmentService _attachmentService;
    private readonly IReturnStateProvider _stateProvider;
    private readonly IReturnSettingsService _settingsService;
    private readonly AbstractValidator<ReturnRequestValidationContext> _requestValidator;

    public ReturnFlowService(
        ICustomerOrderService orderService,
        IReturnService returnService,
        IReturnEligibilityService eligibilityService,
        IReturnAttachmentService attachmentService,
        IReturnStateProvider stateProvider,
        IReturnSettingsService settingsService,
        AbstractValidator<ReturnRequestValidationContext> requestValidator)
    {
        _orderService = orderService;
        _returnService = returnService;
        _eligibilityService = eligibilityService;
        _attachmentService = attachmentService;
        _stateProvider = stateProvider;
        _settingsService = settingsService;
        _requestValidator = requestValidator;
    }

    public virtual async Task<Return> CreateDraft(CreateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var order = await _orderService.GetNoCloneAsync(request.OrderId)
            ?? throw new ReturnFlowException(ReturnFlowError.OrderNotFound, $"Order '{request.OrderId}' was not found.");

        if (!order.CustomerId.EqualsIgnoreCase(context.CustomerId))
        {
            throw new ReturnFlowException(ReturnFlowError.OrderNotFound, $"Order '{request.OrderId}' was not found.");
        }

        var eligibility = await _eligibilityService.GetOrderEligibility(order);

        if (!eligibility.IsEligible)
        {
            throw new ReturnFlowException(ReturnFlowError.OrderNotEligible, eligibility.Reason);
        }

        if (request.Items.IsNullOrEmpty())
        {
            throw new ReturnFlowException(ReturnFlowError.NoItems, "A return needs at least one line.");
        }

        ValidateNoDuplicateLines(request.Items);
        await ValidateRequestAsync(order.StoreId, request.CustomerReference, request.CustomerComment, request.Items);

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

        // Before the first save: a file the buyer cannot attach used to surface after the draft was
        // committed, leaving an invisible return - and one more on every retry.
        await _attachmentService.ValidateAvailable(result, request.Items.SelectMany(x => x.AttachmentUrls ?? []));

        await _returnService.SaveChangesAsync([result]);

        if (!request.Items.Any(x => x.AttachmentUrls != null))
        {
            return result;
        }

        // Saving resolves primary keys but not foreign keys, so the assembled instance cannot be
        // patched back over the persisted rows.
        var saved = await _returnService.GetByIdAsync(result.Id);

        var changes = await UpdateAttachments(saved, request.Items);

        await _returnService.SaveChangesAsync([saved]);
        await ApplyAttachmentChanges(changes);

        return saved;
    }

    public virtual async Task<Return> UpdateDraft(UpdateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var orderReturn = await GetEditableDraftAsync(request.ReturnId, context);
        var order = await _orderService.GetNoCloneAsync(orderReturn.OrderId);
        var orderLineItems = (order?.Items ?? []).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var changes = AbstractTypeFactory<ReturnAttachmentChanges>.TryCreateInstance();

        // Ahead of the header assignment, and outside the items branch: a request that only renames
        // the reference still writes it, so it still has to pass.
        await ValidateRequestAsync(orderReturn.StoreId, request.CustomerReference, request.CustomerComment, request.Items);

        // null leaves the field alone, an empty string clears it - the same rule the attachment
        // list already documents on this input.
        orderReturn.CustomerReference = request.CustomerReference ?? orderReturn.CustomerReference;
        orderReturn.CustomerComment = request.CustomerComment ?? orderReturn.CustomerComment;

        if (request.Items != null)
        {
            if (request.Items.Count == 0)
            {
                throw new ReturnFlowException(ReturnFlowError.NoItems, "A return needs at least one line.");
            }

            ValidateNoDuplicateLines(request.Items);

            var keptLineItems = request.Items.Select(x => UpdateLineItem(x, orderReturn, orderLineItems)).ToList();

            changes = await ReleaseAttachmentsOfDroppedLinesAsync(orderReturn, keptLineItems);

            orderReturn.LineItems = keptLineItems;

            Merge(changes, await UpdateAttachments(orderReturn, request.Items));
        }

        // The return goes first: a failure there must not leave a file reassigned.
        await _returnService.SaveChangesAsync([orderReturn]);
        await ApplyAttachmentChanges(changes);

        return orderReturn;
    }

    // A line the buyer removed takes its rows with it, but the files themselves would stay stamped
    // with this return and never come back to the pool.
    protected virtual async Task<ReturnAttachmentChanges> ReleaseAttachmentsOfDroppedLinesAsync(Return orderReturn, IList<ReturnLineItem> keptLineItems)
    {
        var result = AbstractTypeFactory<ReturnAttachmentChanges>.TryCreateInstance();

        var dropped = (orderReturn.LineItems ?? [])
            .Where(x => !keptLineItems.Contains(x))
            .ToList();

        foreach (var lineItem in dropped)
        {
            Merge(result, await _attachmentService.UpdateAttachments(orderReturn, lineItem, []));
        }

        return result;
    }

    protected virtual async Task<ReturnAttachmentChanges> UpdateAttachments(Return orderReturn, IList<CreateReturnItemRequest> items)
    {
        var result = AbstractTypeFactory<ReturnAttachmentChanges>.TryCreateInstance();

        foreach (var item in items.Where(x => x.AttachmentUrls != null))
        {
            var lineItem = orderReturn.LineItems
                .FirstOrDefault(x => x.OrderLineItemId.EqualsIgnoreCase(item.OrderLineItemId));

            if (lineItem != null)
            {
                Merge(result, await _attachmentService.UpdateAttachments(orderReturn, lineItem, item.AttachmentUrls));
            }
        }

        return result;
    }

    protected static void Merge(ReturnAttachmentChanges target, ReturnAttachmentChanges source)
    {
        foreach (var file in source.Claimed)
        {
            target.Claimed.Add(file);
        }

        foreach (var file in source.Released)
        {
            target.Released.Add(file);
        }
    }

    // A file the buyer dropped from one line may still hang off another, so what to delete can only
    // be decided once every line has been processed - and the service that mints attachment URLs is
    // the one that can tell which files are still referenced.
    protected virtual async Task ApplyAttachmentChanges(ReturnAttachmentChanges changes)
    {
        await _attachmentService.SaveFiles(changes.Claimed);
        await _attachmentService.DeleteUnreferencedFiles(changes.Released);
    }

    public virtual async Task<Return> Submit(string returnId, ReturnFlowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderReturn = await GetEditableDraftAsync(returnId, context);

        if (!_stateProvider.IsAllowed(ReturnAction.Submit, orderReturn.Status))
        {
            throw new ReturnFlowException(
                ReturnFlowError.WrongStatus,
                $"Return '{returnId}' is '{orderReturn.Status}' and cannot be submitted.");
        }

        if (orderReturn.LineItems.IsNullOrEmpty())
        {
            throw new ReturnFlowException(ReturnFlowError.NoItems, "A return needs at least one line.");
        }

        var order = await _orderService.GetNoCloneAsync(orderReturn.OrderId)
            ?? throw new ReturnFlowException(ReturnFlowError.OrderNotFound, $"Order '{orderReturn.OrderId}' was not found.");

        // Drafts reach this point from the admin REST endpoint and from before this validator existed,
        // so the rules cannot live in the writers alone. Submit is where the buyer loses the ability
        // to fix anything, hence also where a reason stops being optional.
        await ValidateRequestAsync(orderReturn.StoreId, orderReturn.CustomerReference, orderReturn.CustomerComment,
            orderReturn.LineItems.Select(ToRequest).ToList(), requireReason: true);

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

    public virtual async Task<Return> Cancel(string returnId, string reason, ReturnFlowContext context, CancellationToken cancellationToken = default)
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

    protected static CreateReturnItemRequest ToRequest(ReturnLineItem lineItem)
    {
        var result = AbstractTypeFactory<CreateReturnItemRequest>.TryCreateInstance();

        result.OrderLineItemId = lineItem.OrderLineItemId;
        result.Quantity = lineItem.Quantity;
        result.ReasonCode = lineItem.ReasonCode;
        result.ReasonComment = lineItem.ReasonComment;
        result.SerialNumber = lineItem.SerialNumber;

        return result;
    }

    protected virtual async Task ValidateAttachmentsAsync(Return orderReturn)
    {
        var rules = await _settingsService.GetRulesAsync(orderReturn.StoreId);

        if (!rules.AttachmentsRequired)
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

    protected virtual async Task ValidateRequestAsync(string storeId, string customerReference, string customerComment, IList<CreateReturnItemRequest> items, bool requireReason = false)
    {
        var rules = await _settingsService.GetRulesAsync(storeId);

        var validationContext = AbstractTypeFactory<ReturnRequestValidationContext>.TryCreateInstance();
        validationContext.CustomerReference = customerReference;
        validationContext.CustomerComment = customerComment;
        validationContext.Items = items ?? [];
        validationContext.Reasons = rules.Reasons;
        validationContext.ReasonsRequiringComment = rules.ReasonsRequiringComment;
        validationContext.RequireReason = requireReason;

        var validation = await _requestValidator.ValidateAsync(validationContext);

        if (!validation.IsValid)
        {
            throw new ReturnFlowException(
                ReturnFlowError.InvalidRequest,
                string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)));
        }
    }

    protected virtual void ValidateNoDuplicateLines(IList<CreateReturnItemRequest> items)
    {
        var duplicate = items
            .GroupBy(x => x.OrderLineItemId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicate != null)
        {
            throw new ReturnFlowException(
                ReturnFlowError.DuplicateLine,
                $"Order line item '{duplicate.Key}' is listed more than once. Ask for the total on a single line.");
        }
    }

    protected virtual async Task ValidateAvailabilityAsync(Return orderReturn, CustomerOrder order)
    {
        var returnableItems = (await _eligibilityService.GetReturnableItems(order, orderReturn.Id))
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

            if (lineItem.Quantity < 1)
            {
                throw new ReturnFlowException(
                    ReturnFlowError.InvalidQuantity,
                    $"Line item '{lineItem.OrderLineItemId}': asked for {lineItem.Quantity}.");
            }
        }

        // Summed per order line: the quantity service aggregates the same way, so validating each
        // row on its own would let a draft claim the full amount once per row.
        foreach (var group in orderReturn.LineItems.GroupBy(x => x.OrderLineItemId ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            var asked = group.Sum(x => x.Quantity);
            var available = returnableItems[group.Key].ReturnableQuantity;

            if (asked > available)
            {
                throw new ReturnFlowException(
                    ReturnFlowError.QuantityUnavailable,
                    $"Line item '{group.Key}': asked for {asked}, {available} available.");
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

        if (orderReturn == null || !await IsOwnedBy(orderReturn, context.CustomerId))
        {
            throw new ReturnFlowException(ReturnFlowError.ReturnNotFound, $"Return '{returnId}' was not found.");
        }

        return orderReturn;
    }

    public virtual async Task<bool> IsOwnedBy(Return orderReturn, string customerId)
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
