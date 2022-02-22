using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;

namespace VirtoCommerce.ReturnModule.Core.Services
{
    public interface IReturnSearchService : ISearchService<ReturnSearchCriteria, ReturnSearchResult, Return>
    {
    }
}
