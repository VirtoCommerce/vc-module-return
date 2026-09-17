using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnableItemType : ExtendableGraphType<ReturnableItem>
{
    public ReturnableItemType()
    {
        Field(x => x.OrderLineItemId, nullable: false);
        Field(x => x.ProductId, nullable: true);
        Field(x => x.Sku, nullable: true);
        Field(x => x.Name, nullable: true);
        Field(x => x.ImageUrl, nullable: true);
        Field(x => x.MeasureUnit, nullable: true);
        Field(x => x.OrderedQuantity, nullable: false);
        Field(x => x.DeliveredQuantity, nullable: false).Description("Quantity actually delivered; may be less than ordered while the order is still shipping.");
        Field(x => x.ReturnableQuantity, nullable: false).Description("Still returnable right now: delivered minus what other returns already hold.");
        Field(x => x.IsReturnable, nullable: false);
        Field(x => x.IneligibilityReason, nullable: true).Description("Reason code to localize on the storefront; null when the line is returnable.");
        Field(x => x.DeliveryDate, nullable: true).Description("When the buyer received the line; the latest date when several shipments carry it.");
        Field(x => x.ReturnableUntil, nullable: true).Description("End of the return window — render the \"N days remaining\" hint from it.");
    }
}
