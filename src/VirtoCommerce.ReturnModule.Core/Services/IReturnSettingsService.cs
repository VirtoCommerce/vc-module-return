using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnSettingsService
{
    Task<ReturnStoreRules> GetRulesAsync(string storeId);
}
