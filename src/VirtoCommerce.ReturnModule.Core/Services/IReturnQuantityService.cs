using System.Collections.Generic;
using System.Threading.Tasks;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnQuantityService
{
    Task<IDictionary<string, int>> GetHeldQuantitiesAsync(string orderId, string excludeReturnId = null);
}
