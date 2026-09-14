using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

/// <summary>
/// Single owner of "may this be returned?" — order-level and line-level. Kept apart from the
/// flow and the quantity engine on purpose: the rules here are the ones most likely to be
/// replaced per project, so an override of this service is the intended extension point.
/// </summary>
public interface IReturnEligibilityService
{
    /// <summary>
    /// Whether the order itself allows a return: its status must be listed in the store's
    /// Return.AllowedOrderStatuses.
    /// </summary>
    Task<ReturnEligibility> GetOrderEligibilityAsync(CustomerOrder order);

    /// <summary>
    /// Every line of the order with its returnable quantity and, when it cannot be returned,
    /// the reason. Ineligible lines are returned too — the buyer has to see them greyed out.
    /// </summary>
    /// <param name="excludeReturnId">
    /// Return to leave out of the held quantities, so a draft being submitted does not count
    /// against itself.
    /// </param>
    Task<IList<ReturnableItem>> GetReturnableItemsAsync(CustomerOrder order, string excludeReturnId = null);
}
