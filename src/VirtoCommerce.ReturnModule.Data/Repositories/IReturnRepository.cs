using System;
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

        Task<ICollection<ReturnEntity>> GetReturnsByIdsAsync(IEnumerable<string> returnIds, string responseGroup = null);
    }
}
