using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnPolicyQueryBuilder : QueryBuilder<ReturnPolicyQuery, ReturnPolicy, ReturnPolicyType>
{
    protected override string Name => "returnPolicy";

    public ReturnPolicyQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnPolicyQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        // Publishes store settings that are not marked IsPublic, so it is not for anonymous callers.
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }
    }
}
