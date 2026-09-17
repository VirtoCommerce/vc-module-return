using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnEligibilityService
{
    Task<ReturnEligibility> GetOrderEligibility(CustomerOrder order);

    Task<IList<ReturnableItem>> GetReturnableItems(CustomerOrder order, string excludeReturnId = null);
}
