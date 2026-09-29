using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQueryBuilder : ReturnSearchQueryBuilderBase<ReturnsQuery, ReturnSearchResult, Return, ReturnType>
{
    protected override string Name => "returns";

    public ReturnsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetOwnCustomerId();
    }
}
