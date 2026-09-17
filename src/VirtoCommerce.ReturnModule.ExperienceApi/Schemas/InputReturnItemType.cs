using GraphQL.Types;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class InputReturnItemType : InputObjectGraphType<CreateReturnItemRequest>
{
    public InputReturnItemType()
    {
        Field(x => x.OrderLineItemId, nullable: false);
        Field(x => x.Quantity, nullable: false);
        Field(x => x.ReasonCode, nullable: true);
        Field(x => x.ReasonComment, nullable: true);
        Field(x => x.SerialNumber, nullable: true);
        Field<ListGraphType<NonNullGraphType<StringGraphType>>>("attachmentUrls")
            .Description("Files already uploaded into the return attachments scope. Omit to leave the line's files alone; pass a list to make them match it exactly.");
    }
}
