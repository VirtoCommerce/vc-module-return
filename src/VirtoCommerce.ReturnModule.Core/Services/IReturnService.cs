using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services
{
    public interface IReturnService : ICrudService<Return>
    {
        Task<Dictionary<string, int>> GetItemsAvailableQuantities(string orderId);
        Task<Dictionary<string, int>> GetItemsAvailableQuantities(CustomerOrder order, string returnId = null);
    }
}
