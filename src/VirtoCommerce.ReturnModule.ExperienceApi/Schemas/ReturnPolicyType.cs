using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnPolicyType : ExtendableGraphType<ReturnPolicy>
{
    public ReturnPolicyType()
    {
        Field(x => x.IsEnabled, nullable: false).Description("Whether returns are enabled for the store.");
        Field(x => x.WindowDays, nullable: false).Description("How long after delivery a line stays returnable, in days.");
    }
}
