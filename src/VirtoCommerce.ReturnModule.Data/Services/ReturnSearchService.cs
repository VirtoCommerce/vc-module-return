using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;

namespace VirtoCommerce.ReturnModule.Data.Services
{
    public class ReturnSearchService : SearchService<ReturnSearchCriteria, ReturnSearchResult, Return, ReturnEntity>, IReturnSearchService
    {
        protected static readonly string[] SortableColumns =
        [
            nameof(ReturnEntity.CreatedDate),
            nameof(ReturnEntity.ModifiedDate),
            nameof(ReturnEntity.CreatedBy),
            nameof(ReturnEntity.Number),
            nameof(ReturnEntity.OrderNumber),
            nameof(ReturnEntity.Status),
            nameof(ReturnEntity.CustomerName),
            nameof(ReturnEntity.CustomerReference),
        ];

        public ReturnSearchService(
            Func<IReturnRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IReturnService crudService,
            IOptions<CrudOptions> crudOptions)
            : base(repositoryFactory, platformMemoryCache, crudService, crudOptions)
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

            if (!criteria.OrderIds.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.OrderIds.Contains(x.OrderId));
            }

            if (!string.IsNullOrEmpty(criteria.CustomerId))
            {
                query = query.Where(x => x.CustomerId == criteria.CustomerId);
            }

            if (!string.IsNullOrEmpty(criteria.StoreId))
            {
                query = query.Where(x => x.StoreId == criteria.StoreId);
            }

            if (!criteria.Statuses.IsNullOrEmpty())
            {
                var statuses = ExpandStatuses(criteria.Statuses);
                query = query.Where(x => statuses.Contains(x.Status));
            }

            if (criteria.StartDate != null)
            {
                query = query.Where(x => x.CreatedDate >= criteria.StartDate);
            }

            if (criteria.EndDate != null)
            {
                query = query.Where(x => x.CreatedDate <= criteria.EndDate);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(ReturnSearchCriteria criteria)
        {
            // An unknown column would otherwise be dropped silently by ApplyOrder, leaving the caller
            // with rows ordered by Id and no hint that the requested sort was ignored.
            var sortInfos = criteria.SortInfos
                .Where(x => SortableColumns.Contains(x.SortColumn, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (sortInfos.Count == 0)
            {
                sortInfos =
                [
                    new SortInfo { SortColumn = nameof(ReturnEntity.CreatedDate), SortDirection = SortDirection.Descending }
                ];
            }

            return sortInfos;
        }

        // The shipped dictionary has always offered both spellings of cancelled, so rows written
        // before the buyer flow carry the other one. Asking for either must find both.
        protected virtual IList<string> ExpandStatuses(IList<string> statuses)
        {
            var result = new List<string>(statuses);

            foreach (var group in StatusSynonyms)
            {
                if (result.Any(x => group.Contains(x, StringComparer.OrdinalIgnoreCase)))
                {
                    result.AddRange(group.Except(result, StringComparer.OrdinalIgnoreCase));
                }
            }

            return result;
        }

        protected virtual IList<string[]> StatusSynonyms { get; } =
        [
            [ReturnStatus.Cancelled, "Canceled"],
        ];

        protected virtual Expression<Func<ReturnEntity, bool>> GetKeywordPredicate(ReturnSearchCriteria criteria)
        {
            var keyword = criteria.Keyword;

            return x => x.Number.Contains(keyword) ||
                        x.OrderNumber.Contains(keyword) ||
                        x.CustomerReference.Contains(keyword) ||
                        x.LineItems.Any(i => i.Sku.Contains(keyword) || i.Name.Contains(keyword));
        }
    }
}
