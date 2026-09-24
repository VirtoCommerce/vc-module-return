using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Queries;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnsQueryHandlerTests
{
    [Fact]
    public async Task Handle_CarriesEveryArgumentOntoTheCriteria()
    {
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = new ReturnsQuery
        {
            CustomerId = "buyer-1",
            StoreId = "B2B-store",
            Statuses = [ReturnStatus.Requested],
            StartDate = startDate,
            EndDate = endDate,
            Keyword = "RET-42",
            Sort = "createdDate:desc",
            Skip = 20,
            Take = 10,
        };

        ReturnSearchCriteria criteria = null;

        var searchService = new Mock<IReturnSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ReturnSearchCriteria>(), false))
            .Callback((ReturnSearchCriteria x, bool _) => criteria = x)
            .ReturnsAsync(new ReturnSearchResult());

        await new ReturnsQueryHandler(searchService.Object).Handle(query, CancellationToken.None);

        Assert.Equal("buyer-1", criteria.CustomerId);
        Assert.Equal("B2B-store", criteria.StoreId);
        Assert.Equal([ReturnStatus.Requested], criteria.Statuses);
        Assert.Equal(startDate, criteria.StartDate);
        Assert.Equal(endDate, criteria.EndDate);
        Assert.Equal("RET-42", criteria.Keyword);
        Assert.Equal("createdDate:desc", criteria.Sort);
        Assert.Equal(20, criteria.Skip);
        Assert.Equal(10, criteria.Take);
        Assert.Null(criteria.OrganizationId);
    }

    [Fact]
    public async Task Handle_OrganizationScope_FiltersByOrganizationInsteadOfBuyer()
    {
        var query = new ReturnsQuery
        {
            CustomerId = "buyer-1",
            StoreId = "B2B-store",
            Scope = ReturnScope.Organization,
            OrganizationId = "org-1",
        };

        ReturnSearchCriteria criteria = null;

        var searchService = new Mock<IReturnSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ReturnSearchCriteria>(), false))
            .Callback((ReturnSearchCriteria x, bool _) => criteria = x)
            .ReturnsAsync(new ReturnSearchResult());

        await new ReturnsQueryHandler(searchService.Object).Handle(query, CancellationToken.None);

        Assert.Equal("org-1", criteria.OrganizationId);
        Assert.Null(criteria.CustomerId);
        Assert.Equal("buyer-1", criteria.DraftsOfCustomerId);
        Assert.Equal("B2B-store", criteria.StoreId);
    }

    [Fact]
    public async Task Handle_OrganizationScopeWithoutOrganization_SearchesNothing()
    {
        var query = new ReturnsQuery
        {
            CustomerId = "buyer-1",
            StoreId = "B2B-store",
            Scope = ReturnScope.Organization,
        };

        var searchService = new Mock<IReturnSearchService>();

        var result = await new ReturnsQueryHandler(searchService.Object).Handle(query, CancellationToken.None);

        Assert.Empty(result.Results);
        searchService.Verify(x => x.SearchAsync(It.IsAny<ReturnSearchCriteria>(), It.IsAny<bool>()), Times.Never);
    }
}
