using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnReasonType : ExtendableGraphType<ReturnReason>
{
    public ReturnReasonType()
    {
        Field(x => x.Code, nullable: false);
        Field(x => x.LocalizedName, nullable: false).Description("Falls back to the code when a store added a value without a translation.");
        Field(x => x.RequiresComment, nullable: false);
    }
}
