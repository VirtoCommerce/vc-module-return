using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
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
        var criteria = request.GetSearchCriteria<ReturnSearchCriteria>();
        criteria.CustomerId = request.CustomerId;
        criteria.StoreId = request.StoreId;
        criteria.Statuses = request.Statuses;
        criteria.StartDate = request.StartDate;
        criteria.EndDate = request.EndDate;

        return await _returnSearchService.SearchNoCloneAsync(criteria);
    }
}
