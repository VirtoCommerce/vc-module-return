using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Data.Models;

namespace VirtoCommerce.ReturnModule.Data.Repositories
{
    public interface IReturnRepository : IRepository
    {
        IQueryable<ReturnEntity> Returns { get; }

        IQueryable<ReturnLineItemEntity> ReturnLineItems { get; }

        Task<IList<ReturnEntity>> GetReturnsByIdsAsync(IList<string> ids, string responseGroup = null);
    }
}
