using GraphQL.Types;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnScopeType : EnumerationGraphType<ReturnScope>
{
    public ReturnScopeType()
    {
        Name = "ReturnScopeEnum";
        Description = "Whose returns to list: the caller's own, or everyone's in the caller's current organization.";
    }
}
