using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQueryHandler : IQueryHandler<ReturnsQuery, ReturnSearchResult>
{
    private readonly IReturnSearchService _returnSearchService;

    public ReturnsQueryHandler(IReturnSearchService returnSearchService)
    {
        _returnSearchService = returnSearchService;
    }

    public virtual async Task<ReturnSearchResult> Handle(ReturnsQuery request, CancellationToken cancellationToken)
    {
        // Neither owner filter set would have the search service return the whole store.
        if (request.Scope == ReturnScope.Organization && string.IsNullOrEmpty(request.OrganizationId))
        {
            return AbstractTypeFactory<ReturnSearchResult>.TryCreateInstance();
        }

        var criteria = request.GetSearchCriteria<ReturnSearchCriteria>();
        // ReturnType has no order field; without this the default response group loads one per row
        // and forces a clone, undoing SearchNoCloneAsync.
        criteria.ResponseGroup = ReturnResponseGroup.None.ToString();
        if (request.Scope == ReturnScope.Organization)
        {
            criteria.OrganizationId = request.OrganizationId;
            criteria.DraftsOfCustomerId = request.CustomerId;
        }
        else
        {
            criteria.CustomerId = request.CustomerId;
        }

        criteria.StoreId = request.StoreId;
        criteria.Statuses = request.Statuses;
        criteria.StartDate = request.StartDate;
        criteria.EndDate = request.EndDate;

        return await _returnSearchService.SearchNoCloneAsync(criteria);
    }
}
