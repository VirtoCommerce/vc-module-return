using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Repositories;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnQuantityService : IReturnQuantityService
{
    private readonly Func<IReturnRepository> _repositoryFactory;
    private readonly IReturnService _returnService;

    public ReturnQuantityService(Func<IReturnRepository> repositoryFactory, IReturnService returnService)
    {
        _repositoryFactory = repositoryFactory;
        _returnService = returnService;
    }

    public virtual async Task<IDictionary<string, int>> GetHeldQuantitiesAsync(string orderId, string excludeReturnId = null)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            return new Dictionary<string, int>();
        }

        List<string> returnIds;

        using (var repository = _repositoryFactory())
        {
            var query = repository.Returns.Where(x => x.OrderId == orderId);

            if (!string.IsNullOrEmpty(excludeReturnId))
            {
                query = query.Where(x => x.Id != excludeReturnId);
            }

            returnIds = query.Select(x => x.Id).ToList();
        }

        if (returnIds.Count == 0)
        {
            return new Dictionary<string, int>();
        }

        // None, not the default WithOrders: summing quantities never needs the hydrated orders.
        var returns = await _returnService.GetAsync(returnIds, ReturnResponseGroup.None.ToString());

        return returns
            .Where(HoldsQuantity)
            .SelectMany(x => x.LineItems ?? Array.Empty<ReturnLineItem>())
            .Where(x => !string.IsNullOrEmpty(x.OrderLineItemId))
            .GroupBy(x => x.OrderLineItemId)
            .ToDictionary(x => x.Key, x => x.Sum(lineItem => lineItem.Quantity));
    }

    /// <summary>
    /// Whether a return still consumes returnable quantity. Before this existed every return was
    /// counted regardless of status, so a cancelled one ate its quantity forever.
    /// </summary>
    /// <remarks>
    /// Iteration 1 runs on the legacy Return.Status dictionary, where cancelling is the only way
    /// quantity comes back. When the Draft / Requested / Approved / PartiallyApproved / Rejected
    /// model arrives, only this method and <see cref="ReleasingStatuses"/> change: Draft holds
    /// nothing, Rejected releases fully, and a partial approval releases the difference.
    /// </remarks>
    protected virtual bool HoldsQuantity(Return orderReturn)
    {
        return !ReleasingStatuses.Contains(orderReturn.Status ?? string.Empty);
    }

    /// <summary>
    /// Statuses that free the quantity back up. Both spellings are listed on purpose: the legacy
    /// status dictionary ships "Canceled", the new status model spells it "Cancelled".
    /// </summary>
    protected virtual ISet<string> ReleasingStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Canceled", "Cancelled" };
}
