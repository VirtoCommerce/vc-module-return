using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnActionType : ExtendableGraphType<ReturnFlowAction>
{
    public ReturnActionType()
    {
        Field(x => x.Name, nullable: false)
            .Description("Stable action code: edit, submit, cancel. The storefront localizes it.");

        Field(x => x.IsAvailable, nullable: false);

        Field(x => x.UnavailableReason, nullable: true)
            .Description("The code the mutation would fail with; null while the action is available.");
    }
}
