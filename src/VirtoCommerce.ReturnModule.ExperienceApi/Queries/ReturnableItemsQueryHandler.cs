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
        // Already loaded and authorized in the builder; the platform caches orders, so this is cheap.
        var order = await _orderService.GetNoCloneAsync(request.OrderId);

        return order == null
            ? []
            : await _eligibilityService.GetReturnableItemsAsync(order);
    }
}
