using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQueryHandler : IQueryHandler<ReturnsQuery, ReturnSearchResult>, IQueryHandler<OrganizationReturnsQuery, ReturnSearchResult>
{
    private readonly IReturnSearchService _returnSearchService;

    public ReturnsQueryHandler(IReturnSearchService returnSearchService)
    {
        _returnSearchService = returnSearchService;
    }

    public virtual async Task<ReturnSearchResult> Handle(ReturnsQuery request, CancellationToken cancellationToken)
    {
        var criteria = GetSearchCriteria(request);
        criteria.CustomerId = request.CustomerId;

        return await _returnSearchService.SearchNoCloneAsync(criteria);
    }

    public virtual async Task<ReturnSearchResult> Handle(OrganizationReturnsQuery request, CancellationToken cancellationToken)
    {
        // Without an owner filter the search service would return the whole store.
        if (string.IsNullOrEmpty(request.OrganizationId))
        {
            return AbstractTypeFactory<ReturnSearchResult>.TryCreateInstance();
        }

        var criteria = GetSearchCriteria(request);
        criteria.OrganizationId = request.OrganizationId;
        criteria.ExcludeDrafts = true;

        return await _returnSearchService.SearchNoCloneAsync(criteria);
    }

    protected virtual ReturnSearchCriteria GetSearchCriteria(ReturnsQuery request)
    {
        var criteria = request.GetSearchCriteria<ReturnSearchCriteria>();
        // ReturnType has no order field; without this the default response group loads one per row
        // and forces a clone, undoing SearchNoCloneAsync.
        criteria.ResponseGroup = ReturnResponseGroup.None.ToString();
        criteria.StoreId = request.StoreId;
        criteria.Statuses = request.Statuses;
        criteria.StartDate = request.StartDate;
        criteria.EndDate = request.EndDate;

        return criteria;
    }
}
