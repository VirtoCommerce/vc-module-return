using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Queries;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnsQueryHandlerTests
{
    private static readonly DateTime StartDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndDate = new(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    private readonly Mock<IReturnSearchService> _searchService = new();
    private ReturnSearchCriteria _criteria;

    public ReturnsQueryHandlerTests()
    {
        _searchService
            .Setup(x => x.SearchAsync(It.IsAny<ReturnSearchCriteria>(), false))
            .Callback((ReturnSearchCriteria x, bool _) => _criteria = x)
            .ReturnsAsync(new ReturnSearchResult());
    }

    [Fact]
    public async Task Handle_CarriesEveryArgumentOntoTheCriteria()
    {
        var query = new ReturnsQuery { CustomerId = "buyer-1" };
        FillSharedArguments(query);

        await new ReturnsQueryHandler(_searchService.Object).Handle(query, CancellationToken.None);

        Assert.Equal("buyer-1", _criteria.CustomerId);
        AssertSharedArguments();
        Assert.Null(_criteria.OrganizationId);
        // The buyer's own list is where their drafts are continued, so it keeps them.
        Assert.False(_criteria.ExcludeDrafts);
    }

    [Fact]
    public async Task Handle_OrganizationReturns_FiltersByOrganizationInsteadOfBuyer()
    {
        var query = new OrganizationReturnsQuery { CustomerId = "buyer-1", OrganizationId = "org-1" };
        FillSharedArguments(query);

        await new ReturnsQueryHandler(_searchService.Object).Handle(query, CancellationToken.None);

        Assert.Equal("org-1", _criteria.OrganizationId);
        Assert.Null(_criteria.CustomerId);
        // Read-only, so no drafts at all - the caller's own included, which stay in their own list.
        Assert.True(_criteria.ExcludeDrafts);
        // Both lists answer the same filters, sorting and paging.
        AssertSharedArguments();
    }

    private static void FillSharedArguments(ReturnsQuery query)
    {
        query.StoreId = "B2B-store";
        query.Statuses = [ReturnStatus.Requested];
        query.StartDate = StartDate;
        query.EndDate = EndDate;
        query.Keyword = "RET-42";
        query.Sort = "createdDate:desc";
        query.Skip = 20;
        query.Take = 10;
    }

    private void AssertSharedArguments()
    {
        Assert.Equal("B2B-store", _criteria.StoreId);
        Assert.Equal([ReturnStatus.Requested], _criteria.Statuses);
        Assert.Equal(StartDate, _criteria.StartDate);
        Assert.Equal(EndDate, _criteria.EndDate);
        Assert.Equal("RET-42", _criteria.Keyword);
        Assert.Equal("createdDate:desc", _criteria.Sort);
        Assert.Equal(20, _criteria.Skip);
        Assert.Equal(10, _criteria.Take);
    }
}
