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

    public virtual async Task<IDictionary<string, int>> GetHeldQuantities(string orderId, string excludeReturnId = null)
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

    protected virtual int GetHeldQuantity(Return orderReturn, ReturnLineItem lineItem)
    {
        var status = orderReturn.Status ?? string.Empty;

        if (NonHoldingStatuses.Contains(status))
        {
            return 0;
        }

        // "Approved" is also a legacy Return.Status value, and nothing writes ApprovedQuantity until
        // the agent side lands, so an admin-approved return would otherwise report zero held.
        return ApprovedStatuses.Contains(status) && IsDecided(lineItem)
            ? lineItem.ApprovedQuantity
            : lineItem.Quantity;
    }

    protected virtual ISet<string> NonHoldingStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.Draft,
            ReturnStatus.Cancelled,
            "Canceled", // legacy dictionary spelling
            ReturnStatus.Rejected,
        };

    protected virtual bool IsDecided(ReturnLineItem lineItem)
    {
        return DecidedItemStates.Contains(lineItem.ItemState ?? string.Empty);
    }

    protected virtual ISet<string> DecidedItemStates { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnItemState.Approved,
            ReturnItemState.Rejected,
        };

    protected virtual ISet<string> ApprovedStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.Approved,
            ReturnStatus.PartiallyApproved,
        };
}
