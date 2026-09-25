using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

/// <summary>
/// One definition of "the buyer" for every channel that tells them about their return.
/// </summary>
public interface IReturnBuyerResolver
{
    /// <summary>
    /// <paramref name="customerId"/> is <see cref="Return.CustomerId"/>: a user id for a return
    /// raised on the storefront, and possibly a member id for one created some other way.
    /// Returns null when neither a member nor an email address can be found.
    /// </summary>
    Task<ReturnBuyer> GetBuyerAsync(string customerId);
}
