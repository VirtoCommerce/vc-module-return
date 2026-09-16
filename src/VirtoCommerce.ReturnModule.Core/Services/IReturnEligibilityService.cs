using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnEligibilityService
{
    Task<ReturnEligibility> GetOrderEligibilityAsync(CustomerOrder order);

    Task<IList<ReturnableItem>> GetReturnableItemsAsync(CustomerOrder order, string excludeReturnId = null);
}
