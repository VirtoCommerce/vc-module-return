using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XOrder.Data.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnableItemsQueryBuilder : QueryBuilder<ReturnableItemsQuery, IList<ReturnableItem>, ListGraphType<ReturnableItemType>>
{
    protected override string Name => "returnableItems";

    public ReturnableItemsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    /// <summary>
    /// The query exposes order line data, so access to the order itself has to be proven first.
    /// Reuses the Orders requirement, which already covers own / organization / anonymous.
    /// </summary>
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnableItemsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        // Builders are singletons — resolve per request, never through the constructor.
        var orderService = context.RequestServices.GetRequiredService<ICustomerOrderService>();
        var order = await orderService.GetNoCloneAsync(request.OrderId);

        await Authorize(context, order, new CanAccessOrderAuthorizationRequirement());
    }
}
