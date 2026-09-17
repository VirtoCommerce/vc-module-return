using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnReasonsQueryBuilder : QueryBuilder<ReturnReasonsQuery, IList<ReturnReason>, ListGraphType<ReturnReasonType>>
{
    protected override string Name => "returnReasons";

    public ReturnReasonsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnReasonsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        // Publishes store settings that are not marked IsPublic, so it is not for anonymous callers.
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }
    }
}
