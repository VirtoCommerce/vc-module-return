using System.Collections.Generic;
using System.Threading.Tasks;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnQuantityService
{
    /// <summary>
    /// Quantity already held by existing returns, per order line item id. A line missing from the
    /// result holds nothing. Returns that release their quantity — cancelled today, plus rejected
    /// once the new status model lands — are not counted.
    /// </summary>
    /// <param name="orderId">Order to look at.</param>
    /// <param name="excludeReturnId">
    /// Return to leave out, so editing an existing return does not compete with itself.
    /// </param>
    Task<IDictionary<string, int>> GetHeldQuantitiesAsync(string orderId, string excludeReturnId = null);
}
