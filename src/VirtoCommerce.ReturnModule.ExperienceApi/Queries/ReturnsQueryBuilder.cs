using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQueryBuilder : SearchQueryBuilder<ReturnsQuery, ReturnSearchResult, Return, ReturnType>
{
    protected override string Name => "returns";

    public ReturnsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    /// <summary>
    /// Scopes the search to the signed-in buyer. The customer is taken from the token rather than
    /// an argument, so there is nothing for a caller to tamper with; an anonymous visitor has no
    /// returns to look at and is refused outright.
    /// </summary>
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetCurrentUserId();

        if (string.IsNullOrEmpty(request.CustomerId))
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }
    }
}
