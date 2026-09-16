using System;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnableItem
{
    public string OrderLineItemId { get; set; }

    public string ProductId { get; set; }

    public string Sku { get; set; }

    public string Name { get; set; }

    public string ImageUrl { get; set; }

    public string MeasureUnit { get; set; }

    public int OrderedQuantity { get; set; }

    public int DeliveredQuantity { get; set; }

    public int ReturnableQuantity { get; set; }

    public bool IsReturnable { get; set; }

    public string IneligibilityReason { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime? ReturnableUntil { get; set; }
}
