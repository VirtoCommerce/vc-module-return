using System;

namespace VirtoCommerce.ReturnModule.Core.Models;

/// <summary>
/// One order line as seen by the "what can I return?" screen. Every line of the order is
/// projected, eligible or not: the buyer needs to see why a line is greyed out.
/// </summary>
public class ReturnableItem
{
    public string OrderLineItemId { get; set; }

    public string ProductId { get; set; }

    public string Sku { get; set; }

    public string Name { get; set; }

    public string ImageUrl { get; set; }

    public string MeasureUnit { get; set; }

    /// <summary>
    /// Quantity on the order line.
    /// </summary>
    public int OrderedQuantity { get; set; }

    /// <summary>
    /// Quantity actually delivered, summed over the shipments carrying this line. May be less
    /// than <see cref="OrderedQuantity"/> while an order is still being shipped.
    /// </summary>
    public int DeliveredQuantity { get; set; }

    /// <summary>
    /// Still returnable right now: delivered minus what other returns already hold. Counted from
    /// the delivered quantity, not the ordered one — what has not arrived cannot come back.
    /// </summary>
    public int ReturnableQuantity { get; set; }

    public bool IsReturnable { get; set; }

    /// <summary>
    /// One of <see cref="ReturnIneligibilityReason"/>, or null when the line is returnable.
    /// </summary>
    public string IneligibilityReason { get; set; }

    /// <summary>
    /// When the buyer received the line. Taken from the shipment carrying it; with several
    /// shipments the latest date wins, because the window must not start before the last
    /// part arrived. Null when no shipment reports a delivery date.
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// End of the return window — <see cref="DeliveryDate"/> plus the store's Return.WindowDays.
    /// The storefront renders the "N days remaining" hint from it.
    /// </summary>
    public DateTime? ReturnableUntil { get; set; }
}
