using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnPolicyQueryBuilder : ReturnQueryBuilderBase<ReturnPolicyQuery, ReturnPolicy, ReturnPolicyType>
{
    protected override string Name => "returnPolicy";

    public ReturnPolicyQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }
}
