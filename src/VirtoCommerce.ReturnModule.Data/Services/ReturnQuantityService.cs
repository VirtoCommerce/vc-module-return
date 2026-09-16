using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core;
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
            .SelectMany(orderReturn => (orderReturn.LineItems ?? Array.Empty<ReturnLineItem>())
                .Where(lineItem => !string.IsNullOrEmpty(lineItem.OrderLineItemId))
                .Select(lineItem => new
                {
                    lineItem.OrderLineItemId,
                    Quantity = GetHeldQuantity(orderReturn, lineItem),
                }))
            .Where(x => x.Quantity > 0)
            .GroupBy(x => x.OrderLineItemId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));
    }

    /// <summary>
    /// How much of an order line this return's line is keeping out of reach.
    /// </summary>
    /// <remarks>
    /// Anything this module does not recognise still holds its full requested quantity. Returns
    /// raised in the admin UI carry values from the editable Return.Status dictionary, which this
    /// module does not control, and under-counting would let a buyer return more than they have.
    /// </remarks>
    protected virtual int GetHeldQuantity(Return orderReturn, ReturnLineItem lineItem)
    {
        var status = orderReturn.Status ?? string.Empty;

        if (NonHoldingStatuses.Contains(status))
        {
            return 0;
        }

        // Ask for 240, get 200, and the other 40 are free again — the buyer may legitimately ask
        // for them later.
        return ApprovedStatuses.Contains(status) ? lineItem.ApprovedQuantity : lineItem.Quantity;
    }

    /// <summary>
    /// Statuses holding nothing: a draft has not claimed anything yet — an abandoned one must not
    /// block a line forever — and a closed return has given back whatever it held.
    /// </summary>
    /// <remarks>
    /// Both spellings of cancelled are listed on purpose: the legacy status dictionary ships
    /// "Canceled", the status model spells it "Cancelled".
    /// </remarks>
    protected virtual ISet<string> NonHoldingStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.Draft,
            ReturnStatus.Cancelled,
            "Canceled",
            ReturnStatus.Rejected,
        };

    /// <summary>
    /// Statuses where an agent has settled the quantity, so the approved figure is what is held.
    /// </summary>
    protected virtual ISet<string> ApprovedStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.Approved,
            ReturnStatus.PartiallyApproved,
        };
}
