using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public virtual async Task<ICollection<ReturnEntity>> GetReturnsByIdsAsync(IEnumerable<string> returnIds, string responseGroup = null)
        {
            var result = await Returns.Where(x => returnIds.Contains(x.Id)).ToListAsync();
            await ReturnLineItems.Where(x => returnIds.Contains(x.ReturnId)).LoadAsync();

            return result;
        }
    }
}
