using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services
{
    public interface IReturnService : ICrudService<Return>
    {
        [Obsolete("Counts the requested quantity of every return, drafts, cancelled and declined ones included. Subtract IReturnQuantityService.GetHeldQuantities instead.", DiagnosticId = "VC0016", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        Task<Dictionary<string, int>> GetItemsAvailableQuantities(string orderId);

        [Obsolete("Counts the requested quantity of every return, drafts, cancelled and declined ones included. Subtract IReturnQuantityService.GetHeldQuantities instead.", DiagnosticId = "VC0016", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        Task<Dictionary<string, int>> GetItemsAvailableQuantities(CustomerOrder order, string returnId = null);
    }
}
