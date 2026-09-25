using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnQuantityService
{
    Task<IDictionary<string, int>> GetHeldQuantities(string orderId, string excludeReturnId = null);

    int GetHeldQuantity(Return orderReturn, ReturnLineItem lineItem);
}
