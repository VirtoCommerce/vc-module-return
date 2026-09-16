using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnQueryHandler : IQueryHandler<ReturnQuery, Return>
{
    private readonly IReturnService _returnService;
    private readonly ICustomerOrderService _orderService;

    public ReturnQueryHandler(IReturnService returnService, ICustomerOrderService orderService)
    {
        _returnService = returnService;
        _orderService = orderService;
    }

    public virtual async Task<Return> Handle(ReturnQuery request, CancellationToken cancellationToken)
    {
        var result = await _returnService.GetNoCloneAsync(request.Id, ReturnResponseGroup.None.ToString());

        return result != null && await IsOwnedByAsync(result, request.CustomerId) ? result : null;
    }

    protected virtual async Task<bool> IsOwnedByAsync(Return orderReturn, string customerId)
    {
        if (!string.IsNullOrEmpty(orderReturn.CustomerId))
        {
            return orderReturn.CustomerId.EqualsIgnoreCase(customerId);
        }

        var order = await _orderService.GetNoCloneAsync(orderReturn.OrderId);

        return order != null && order.CustomerId.EqualsIgnoreCase(customerId);
    }
}
