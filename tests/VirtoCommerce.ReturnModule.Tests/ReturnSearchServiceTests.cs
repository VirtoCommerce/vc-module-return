using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;
using VirtoCommerce.ReturnModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnSearchServiceTests
{
    private static readonly DateTime _created = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("RET-42")]
    [InlineData("SO-2026")]
    [InlineData("PO-7788")]
    [InlineData("ARS-P3265LV")]
    [InlineData("Access control")]
    public void Keyword_MatchesNumberOrderNumberReferenceSkuAndName(string keyword)
    {
        var found = Search(new ReturnSearchCriteria { Keyword = keyword });

        Assert.Equal("r1", Assert.Single(found).Id);
    }

    [Fact]
    public void Keyword_MatchesTheBuyersName()
    {
        // The organization's list is read by people looking for what a colleague sent back.
        var found = Search(new ReturnSearchCriteria { Keyword = "Jan de" });

        Assert.Equal("r1", Assert.Single(found).Id);
    }

    [Fact]
    public void Keyword_StatusCode_MatchesNothing()
    {
        var found = Search(new ReturnSearchCriteria { Keyword = ReturnStatus.Requested });

        Assert.Empty(found);
    }

    [Fact]
    public void Keyword_NoMatch_ReturnsNothing()
    {
        var found = Search(new ReturnSearchCriteria { Keyword = "nothing-like-this" });

        Assert.Empty(found);
    }

    [Fact]
    public void Statuses_FiltersByAnyOfThem()
    {
        var found = Search(new ReturnSearchCriteria { Statuses = [ReturnStatus.Draft] });

        Assert.Equal("r2", Assert.Single(found).Id);
    }

    [Theory]
    [InlineData(ReturnStatus.Cancelled)]
    [InlineData("Canceled")]
    public void Statuses_EitherSpellingOfCancelled_FindsBoth(string asked)
    {
        var found = Search(new ReturnSearchCriteria { Statuses = [asked] });

        Assert.Equal(new[] { "r3", "r4" }, found.Select(x => x.Id).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void Statuses_Empty_AppliesNoFilter()
    {
        var found = Search(new ReturnSearchCriteria { Statuses = [] });

        Assert.Equal(4, found.Count);
    }

    [Fact]
    public void StartDateAndEndDate_AreInclusive()
    {
        var found = Search(new ReturnSearchCriteria { StartDate = _created, EndDate = _created });

        Assert.Equal("r1", Assert.Single(found).Id);
    }

    [Fact]
    public void StartDate_OneTickLate_ExcludesTheReturn()
    {
        var found = Search(new ReturnSearchCriteria { StartDate = _created.AddTicks(1) });

        Assert.DoesNotContain(found, x => x.Id == "r1");
    }

    [Fact]
    public void EndDate_OneTickEarly_ExcludesTheReturn()
    {
        var found = Search(new ReturnSearchCriteria { EndDate = _created.AddTicks(-1) });

        Assert.DoesNotContain(found, x => x.Id == "r1");
    }

    [Fact]
    public void StartDateAlone_IsAnOpenEndedRange()
    {
        var found = Search(new ReturnSearchCriteria { StartDate = _created });

        Assert.Equal(new[] { "r1", "r3", "r4" }, found.Select(x => x.Id).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void EndDateAlone_IsAnOpenEndedRange()
    {
        var found = Search(new ReturnSearchCriteria { EndDate = _created });

        Assert.Equal(new[] { "r1", "r2" }, found.Select(x => x.Id).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void CustomerAndStore_AreCombined()
    {
        var found = Search(new ReturnSearchCriteria { CustomerId = "buyer-1", StoreId = "other-store" });

        Assert.Empty(found);
    }

    [Fact]
    public void Organization_FiltersByIt()
    {
        var found = Search(new ReturnSearchCriteria { OrganizationId = "org-2" });

        Assert.Equal("r2", Assert.Single(found).Id);
    }

    [Fact]
    public void Organization_NoneOfItsReturns_ReturnsNothing()
    {
        var found = Search(new ReturnSearchCriteria { OrganizationId = "org-without-returns" });

        Assert.Empty(found);
    }

    [Fact]
    public void ExcludeDrafts_HidesEveryDraft()
    {
        // The draft belongs to the same buyer as every other return here: being their own does not
        // bring it back.
        var found = Search(new ReturnSearchCriteria { ExcludeDrafts = true });

        Assert.DoesNotContain(found, x => x.Status == ReturnStatus.Draft);
        Assert.Equal(3, found.Count);
    }

    [Fact]
    public void ExcludeDraftsNotSet_KeepsDrafts()
    {
        var found = Search(new ReturnSearchCriteria());

        Assert.Contains(found, x => x.Id == "r2");
        Assert.Equal(4, found.Count);
    }

    [Fact]
    public void OrganizationWithOnlyADraft_ShowsNothingWhenDraftsAreExcluded()
    {
        var found = Search(new ReturnSearchCriteria { OrganizationId = "org-2", ExcludeDrafts = true });

        Assert.Empty(found);
    }

    [Fact]
    public void Sort_NotRequested_FallsBackToNewestFirst()
    {
        var sortInfos = CreateService().Sort(new ReturnSearchCriteria());

        var sortInfo = Assert.Single(sortInfos);
        Assert.Equal(nameof(ReturnEntity.CreatedDate), sortInfo.SortColumn);
        Assert.Equal(SortDirection.Descending, sortInfo.SortDirection);
    }

    [Fact]
    public void Sort_UnknownColumn_FallsBackToNewestFirst()
    {
        var sortInfos = CreateService().Sort(new ReturnSearchCriteria { Sort = "customerComment:asc" });

        var sortInfo = Assert.Single(sortInfos);
        Assert.Equal(nameof(ReturnEntity.CreatedDate), sortInfo.SortColumn);
        Assert.Equal(SortDirection.Descending, sortInfo.SortDirection);
    }

    [Fact]
    public void Sort_KnownColumnsInAnyCase_PassThrough()
    {
        var sortInfos = CreateService().Sort(new ReturnSearchCriteria { Sort = "createdDate:desc;NUMBER" });

        Assert.Equal(2, sortInfos.Count);
        Assert.Equal(SortDirection.Descending, sortInfos[0].SortDirection);
        Assert.Equal(SortDirection.Ascending, sortInfos[1].SortDirection);
    }

    [Fact]
    public void Sort_Organization_PassesThrough()
    {
        // The admin list sorts by it; a column missing from the list would be dropped silently.
        var sortInfos = CreateService().Sort(new ReturnSearchCriteria { Sort = "organizationName:desc" });

        var sortInfo = Assert.Single(sortInfos);
        Assert.Equal(nameof(ReturnEntity.OrganizationName), sortInfo.SortColumn, ignoreCase: true);
        Assert.Equal(SortDirection.Descending, sortInfo.SortDirection);
    }

    private static IList<ReturnEntity> Search(ReturnSearchCriteria criteria)
    {
        var service = CreateService();

        return service.Query(criteria).ToList();
    }

    private static TestableReturnSearchService CreateService()
    {
        // Every searched column must be set: NULL simply fails to match in SQL, but in
        // LINQ-to-objects null.Contains() throws.
        var entities = new List<ReturnEntity>
        {
            MakeReturn("r1", ReturnStatus.Requested, _created,
                number: "RET-42", orderNumber: "SO-2026-04417", customerReference: "PO-7788",
                sku: "ARS-P3265LV", name: "Access control panel", customerName: "Jan de Vries"),
            MakeReturn("r2", ReturnStatus.Draft, _created.AddDays(-1), organizationId: "org-2"),
            MakeReturn("r3", ReturnStatus.Cancelled, _created.AddDays(1)),
            MakeReturn("r4", "Canceled", _created.AddDays(2)),
        }.BuildMock();

        var repository = new Mock<IReturnRepository>();
        repository.Setup(x => x.Returns).Returns(entities);

        return new TestableReturnSearchService(() => repository.Object, repository.Object);
    }

    private static ReturnEntity MakeReturn(
        string id,
        string status,
        DateTime createdDate,
        string number = "RET-000",
        string orderNumber = "SO-000",
        string customerReference = "REF-000",
        string sku = "SKU-000",
        string name = "Item",
        string organizationId = "org-1",
        string customerName = "Buyer")
    {
        return new ReturnEntity
        {
            Id = id,
            Status = status,
            CreatedDate = createdDate,
            Number = number,
            OrderNumber = orderNumber,
            CustomerReference = customerReference,
            CustomerId = "buyer-1",
            CustomerName = customerName,
            OrganizationId = organizationId,
            StoreId = "B2B-store",
            LineItems = [new ReturnLineItemEntity { Sku = sku, Name = name }],
        };
    }

    private sealed class TestableReturnSearchService : ReturnSearchService
    {
        private readonly IRepository _repository;

        public TestableReturnSearchService(Func<IReturnRepository> repositoryFactory, IRepository repository)
            : base(repositoryFactory, null, null, Options.Create(new CrudOptions()))
        {
            _repository = repository;
        }

        public IQueryable<ReturnEntity> Query(ReturnSearchCriteria criteria) => BuildQuery(_repository, criteria);

        public IList<SortInfo> Sort(ReturnSearchCriteria criteria) => BuildSortExpression(criteria);
    }
}
