using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ReturnModule.Data.Models;

namespace VirtoCommerce.ReturnModule.Data.Repositories
{
    public class ReturnRepositoryImpl : DbContextRepositoryBase<ReturnDbContext>, IReturnRepository
    {
        public ReturnRepositoryImpl(ReturnDbContext dbContext, IUnitOfWork unitOfWork = null)
            : base(dbContext, unitOfWork)
        {
        }

        public IQueryable<ReturnEntity> Returns => DbContext.Set<ReturnEntity>();

        public IQueryable<ReturnLineItemEntity> ReturnLineItems => DbContext.Set<ReturnLineItemEntity>();

        public virtual async Task<IList<ReturnEntity>> GetReturnsByIdsAsync(IList<string> ids, string responseGroup = null)
        {
            if (ids.IsNullOrEmpty())
            {
                return Array.Empty<ReturnEntity>();
            }

            var result = await Returns.Where(x => ids.Contains(x.Id)).ToListAsync();

            if (result.Any())
            {
                var existingIds = result.Select(x => x.Id).ToList();
                await ReturnLineItems.Where(x => existingIds.Contains(x.ReturnId)).LoadAsync();
            }

            return result;
        }
    }
}
