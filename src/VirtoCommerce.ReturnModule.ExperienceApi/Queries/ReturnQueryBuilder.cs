using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnQueryBuilder : ReturnQueryBuilderBase<ReturnQuery, Return, ReturnType>
{
    protected override string Name => "return";

    public ReturnQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetOwnCustomerId();
    }
}
