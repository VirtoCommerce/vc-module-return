using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;

namespace VirtoCommerce.ReturnModule.Data.Services
{
    public class ReturnSearchService : SearchService<ReturnSearchCriteria, ReturnSearchResult, Return, ReturnEntity>, IReturnSearchService
    {
        public ReturnSearchService(Func<IReturnRepository> returnRepositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IReturnService returnService)
            : base(returnRepositoryFactory, platformMemoryCache, returnService)
        {
        }

        protected override IQueryable<ReturnEntity> BuildQuery(IRepository repository, ReturnSearchCriteria criteria)
        {
            var query = ((IReturnRepository)repository).Returns;

            if (!criteria.ObjectIds.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.ObjectIds.Contains(x.Id));
            }

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                query = query.Where(GetKeywordPredicate(criteria));
            }

            if (!string.IsNullOrWhiteSpace(criteria.OrderId))
            {
                query = query.Where(x => x.OrderId == criteria.OrderId);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(ReturnSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;
            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos = new[]
                {
                    new SortInfo { SortColumn = nameof(ReturnEntity.Number) }
                };
            }

            return sortInfos;
        }

        protected virtual Expression<Func<ReturnEntity, bool>> GetKeywordPredicate(ReturnSearchCriteria criteria)
        {
            return orderReturn => orderReturn.Number.Contains(criteria.Keyword) || orderReturn.Status.Contains(criteria.Keyword);
        }
    }
}
