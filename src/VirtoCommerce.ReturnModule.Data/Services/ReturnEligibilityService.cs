using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnEligibilityService : IReturnEligibilityService
{
    private static readonly char[] _statusSeparators = [',', ';'];

    private readonly IStoreService _storeService;
    private readonly IReturnQuantityService _quantityService;

    public ReturnEligibilityService(IStoreService storeService, IReturnQuantityService quantityService)
    {
        _storeService = storeService;
        _quantityService = quantityService;
    }

    public virtual async Task<ReturnEligibility> GetOrderEligibilityAsync(CustomerOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var settings = await GetStoreSettingsAsync(order.StoreId);

        var reason = GetOrderLevelReason(order, settings);

        return reason == null ? ReturnEligibility.Eligible() : ReturnEligibility.Ineligible(reason);
    }

    public virtual async Task<IList<ReturnableItem>> GetReturnableItemsAsync(CustomerOrder order, string excludeReturnId = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        var settings = await GetStoreSettingsAsync(order.StoreId);
        var windowDays = settings.GetValue<int>(ModuleConstants.Settings.General.ReturnWindowDays);
        // Settled once for the whole order, so every line reports the same truthful reason rather
        // than blaming the order status for a store that has returns switched off.
        var orderLevelReason = GetOrderLevelReason(order, settings);

        var heldQuantities = await _quantityService.GetHeldQuantitiesAsync(order.Id, excludeReturnId);
        var deliveries = GetLineItemDeliveries(order, settings);

        var result = new List<ReturnableItem>();

        foreach (var lineItem in order.Items ?? [])
        {
            var item = AbstractTypeFactory<ReturnableItem>.TryCreateInstance();

            item.OrderLineItemId = lineItem.Id;
            item.ProductId = lineItem.ProductId;
            item.Sku = lineItem.Sku;
            item.Name = lineItem.Name;
            item.ImageUrl = lineItem.ImageUrl;
            item.MeasureUnit = lineItem.MeasureUnit;
            item.OrderedQuantity = lineItem.Quantity;

            if (deliveries.TryGetValue(lineItem.Id ?? string.Empty, out var delivery))
            {
                item.DeliveredQuantity = delivery.Quantity;
                item.DeliveryDate = delivery.LatestDate;
                item.ReturnableUntil = delivery.LatestDate.AddDays(windowDays);
            }

            heldQuantities.TryGetValue(lineItem.Id ?? string.Empty, out var heldQuantity);
            item.ReturnableQuantity = Math.Max(0, item.DeliveredQuantity - heldQuantity);

            item.IneligibilityReason = GetIneligibilityReason(orderLevelReason, lineItem, item);
            item.IsReturnable = item.IneligibilityReason == null;

            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Why nothing on this order may be returned, or null when the order itself is fine.
    /// </summary>
    protected virtual string GetOrderLevelReason(CustomerOrder order, IEnumerable<ObjectSettingEntry> settings)
    {
        if (!IsReturnEnabled(settings))
        {
            return ReturnIneligibilityReason.ReturnsDisabled;
        }

        return IsOrderStatusAllowed(order, settings) ? null : ReturnIneligibilityReason.OrderStatusNotAllowed;
    }

    protected virtual string GetIneligibilityReason(string orderLevelReason, LineItem lineItem, ReturnableItem item)
    {
        if (orderLevelReason != null)
        {
            return orderLevelReason;
        }

        if (lineItem.IsCancelled)
        {
            return ReturnIneligibilityReason.LineCancelled;
        }

        // No shipment reports a delivery date, so there is no honest point to count the window
        // from. Deliberately not falling back to the order date — see docs/questions.md 5.1.
        if (item.ReturnableUntil == null)
        {
            return ReturnIneligibilityReason.NotDelivered;
        }

        if (item.ReturnableUntil < DateTime.UtcNow)
        {
            return ReturnIneligibilityReason.OutsideReturnWindow;
        }

        if (item.ReturnableQuantity <= 0)
        {
            return ReturnIneligibilityReason.NothingLeftToReturn;
        }

        return null;
    }

    /// <summary>
    /// What was actually delivered per order line item, gathered from the shipments carrying it.
    /// </summary>
    /// <remarks>
    /// A single line can be split across shipments, so quantities add up while the latest date
    /// wins: the window must not start before the buyer received the last part of the line,
    /// otherwise a split delivery would quietly shorten it. Shipments that are cancelled, report
    /// no delivery date, or sit outside Return.AllowedShipmentStatuses count for neither.
    /// </remarks>
    protected virtual IDictionary<string, LineItemDelivery> GetLineItemDeliveries(CustomerOrder order, IEnumerable<ObjectSettingEntry> settings)
    {
        var result = new Dictionary<string, LineItemDelivery>();
        var allowedStatuses = ParseStatuses(settings.GetValue<string>(ModuleConstants.Settings.General.ReturnAllowedShipmentStatuses));

        foreach (var shipment in order.Shipments ?? [])
        {
            if (shipment.DeliveryDate == null || shipment.IsCancelled || !IsShipmentDelivered(shipment, allowedStatuses))
            {
                continue;
            }

            foreach (var shipmentItem in shipment.Items ?? [])
            {
                if (string.IsNullOrEmpty(shipmentItem.LineItemId))
                {
                    continue;
                }

                if (!result.TryGetValue(shipmentItem.LineItemId, out var delivery))
                {
                    delivery = new LineItemDelivery();
                    result[shipmentItem.LineItemId] = delivery;
                }

                delivery.Quantity += shipmentItem.Quantity;

                if (shipment.DeliveryDate.Value > delivery.LatestDate)
                {
                    delivery.LatestDate = shipment.DeliveryDate.Value;
                }
            }
        }

        return result;
    }

    protected class LineItemDelivery
    {
        public DateTime LatestDate { get; set; }

        public int Quantity { get; set; }
    }

    /// <summary>
    /// Whether a shipment counts as delivered. An empty allow-list means any status qualifies —
    /// the delivery date is then the only signal.
    /// </summary>
    protected virtual bool IsShipmentDelivered(Shipment shipment, IList<string> allowedStatuses)
    {
        return allowedStatuses.Count == 0 ||
               allowedStatuses.Contains(shipment.Status ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The store's master switch. Without this check it only hid the storefront's menu, while the
    /// xAPI happily went on accepting returns.
    /// </summary>
    protected virtual bool IsReturnEnabled(IEnumerable<ObjectSettingEntry> settings)
    {
        return settings.GetValue<bool>(ModuleConstants.Settings.General.ReturnEnabled);
    }

    protected virtual bool IsOrderStatusAllowed(CustomerOrder order, IEnumerable<ObjectSettingEntry> settings)
    {
        // Unlike shipments, an empty list allows nothing: Return.ReturnEnabled is the off switch.
        return ParseStatuses(settings.GetValue<string>(ModuleConstants.Settings.General.ReturnAllowedOrderStatuses))
            .Contains(order.Status ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    protected static IList<string> ParseStatuses(string commaSeparatedStatuses)
    {
        return commaSeparatedStatuses?.Split(_statusSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }

    protected virtual async Task<IEnumerable<ObjectSettingEntry>> GetStoreSettingsAsync(string storeId)
    {
        var store = string.IsNullOrEmpty(storeId) ? null : await _storeService.GetNoCloneAsync(storeId);

        // An unknown store falls back to the descriptor defaults rather than failing the read.
        return store?.Settings ?? [];
    }
}
