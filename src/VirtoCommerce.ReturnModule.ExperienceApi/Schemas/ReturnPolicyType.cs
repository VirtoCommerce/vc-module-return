using GraphQL.Types;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnPolicyType : ExtendableGraphType<ReturnPolicy>
{
    public ReturnPolicyType()
    {
        Field(x => x.IsEnabled, nullable: false).Description("Whether returns are enabled for the store.");
        Field(x => x.WindowDays, nullable: false).Description("How long after delivery a line stays returnable, in days.");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("allowedOrderStatuses")
            .Description("Order statuses a return may be raised from. Lets a caller skip asking for returnable items.")
            .Resolve(context => context.Source.AllowedOrderStatuses);
    }
}
