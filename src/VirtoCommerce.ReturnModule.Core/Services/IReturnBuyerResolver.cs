using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnBuyerResolver
{
    // customerId is Return.CustomerId: a user id for a storefront return, possibly a member id for one
    // created some other way. Null when neither a member nor an email address is found.
    Task<ReturnBuyer> GetBuyerAsync(string customerId);
}
