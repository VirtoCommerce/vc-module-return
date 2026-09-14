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

        // Null rather than "forbidden": a buyer probing ids should not learn which ones exist.
        return result != null && await IsOwnedByAsync(result, request.CustomerId) ? result : null;
    }

    /// <summary>
    /// Ownership of a return.
    /// </summary>
    /// <remarks>
    /// New returns carry the buyer themselves. Ones raised in the admin UI before that field
    /// existed do not, so those fall back to the order they belong to.
    /// </remarks>
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
