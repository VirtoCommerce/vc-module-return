using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQueryHandler : IQueryHandler<ReturnsQuery, ReturnSearchResult>
{
    private readonly IReturnSearchService _returnSearchService;
    private readonly ICustomerOrderSearchService _orderSearchService;

    public ReturnsQueryHandler(IReturnSearchService returnSearchService, ICustomerOrderSearchService orderSearchService)
    {
        _returnSearchService = returnSearchService;
        _orderSearchService = orderSearchService;
    }

    /// <summary>
    /// How many of the buyer's orders are considered when resolving "my returns".
    /// </summary>
    /// <remarks>
    /// A stopgap: <c>Return</c> has no customer of its own yet, so ownership is expressed through
    /// the orders. The cap keeps the generated IN clause sane; it disappears together with this
    /// whole lookup once Return.CustomerId exists.
    /// </remarks>
    protected virtual int MaxCustomerOrders => 1000;

    public virtual async Task<ReturnSearchResult> Handle(ReturnsQuery request, CancellationToken cancellationToken)
    {
        var orderIds = await GetCustomerOrderIdsAsync(request.CustomerId);

        if (orderIds.Count == 0)
        {
            return AbstractTypeFactory<ReturnSearchResult>.TryCreateInstance();
        }

        var criteria = request.GetSearchCriteria<ReturnSearchCriteria>();
        criteria.OrderIds = orderIds;
        criteria.Statuses = request.Statuses;

        return await _returnSearchService.SearchNoCloneAsync(criteria);
    }

    protected virtual async Task<IList<string>> GetCustomerOrderIdsAsync(string customerId)
    {
        var criteria = AbstractTypeFactory<CustomerOrderSearchCriteria>.TryCreateInstance();
        criteria.CustomerId = customerId;
        criteria.Take = MaxCustomerOrders;

        var result = await _orderSearchService.SearchNoCloneAsync(criteria);

        return result.Results.Select(x => x.Id).ToList();
    }
}
