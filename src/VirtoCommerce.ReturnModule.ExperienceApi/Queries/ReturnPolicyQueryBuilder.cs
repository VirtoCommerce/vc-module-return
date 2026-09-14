using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnPolicyQueryBuilder : QueryBuilder<ReturnPolicyQuery, ReturnPolicy, ReturnPolicyType>
{
    protected override string Name => "returnPolicy";

    // Store-level rules only, no customer data: intentionally left unauthorized.
    public ReturnPolicyQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }
}
