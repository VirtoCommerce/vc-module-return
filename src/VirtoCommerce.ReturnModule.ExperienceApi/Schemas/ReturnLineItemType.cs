using System.Collections.Generic;
using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnLineItemType : ExtendableGraphType<ReturnLineItem>
{
    public ReturnLineItemType()
    {
        Field(x => x.Id, nullable: false);
        Field(x => x.OrderLineItemId, nullable: true);
        Field(x => x.ProductId, nullable: true);
        Field(x => x.Sku, nullable: true);
        Field(x => x.Name, nullable: true);
        Field(x => x.ImageUrl, nullable: true);
        Field(x => x.MeasureUnit, nullable: true);
        Field(x => x.OrderedQuantity, nullable: false).Description("Quantity on the order line when the return was raised.");
        Field(x => x.Quantity, nullable: false).Description("Quantity the buyer asked to return.");
        Field(x => x.ApprovedQuantity, nullable: false).Description("Quantity an agent authorized; 0 means the line was rejected.");
        Field(x => x.ItemState, nullable: true);
        Field(x => x.ReasonCode, nullable: true);
        Field(x => x.ReasonComment, nullable: true);
        Field(x => x.RejectReason, nullable: true);
        Field(x => x.SerialNumber, nullable: true);

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<ReturnAttachmentType>>>>("attachments")
            .Resolve(context => (IEnumerable<ReturnAttachment>)context.Source.Attachments ?? []);
    }
}
