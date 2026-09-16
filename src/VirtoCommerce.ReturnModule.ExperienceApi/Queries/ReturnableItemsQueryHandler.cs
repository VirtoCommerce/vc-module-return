using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnableItemsQueryHandler : IQueryHandler<ReturnableItemsQuery, IList<ReturnableItem>>
{
    private readonly ICustomerOrderService _orderService;
    private readonly IReturnEligibilityService _eligibilityService;

    public ReturnableItemsQueryHandler(ICustomerOrderService orderService, IReturnEligibilityService eligibilityService)
    {
        _orderService = orderService;
        _eligibilityService = eligibilityService;
    }

    public virtual async Task<IList<ReturnableItem>> Handle(ReturnableItemsQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetNoCloneAsync(request.OrderId);

        // Reading somebody else's order is allowed for an organization colleague, but only its own
        // buyer may return from it - the same rule CreateDraftAsync enforces. Answering with an
        // empty list keeps the storefront from offering a button that would be refused.
        if (order == null || !order.CustomerId.EqualsIgnoreCase(request.CustomerId))
        {
            return [];
        }

        return await _eligibilityService.GetReturnableItemsAsync(order);
    }
}
