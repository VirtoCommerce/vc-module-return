using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Queries;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnQueryHandlerTests
{
    private const string ReturnId = "return-1";
    private const string OwnerId = "buyer-1";
    private const string ColleagueId = "buyer-2";

    [Fact]
    public async Task OwnReturn_IsFound()
    {
        var result = await Handle(new ReturnQuery { Id = ReturnId, CustomerId = OwnerId });

        Assert.Equal(ReturnId, result?.Id);
    }

    [Fact]
    public async Task ColleaguesReturn_WithoutOrganizationAccess_IsNotFound()
    {
        var result = await Handle(new ReturnQuery { Id = ReturnId, CustomerId = ColleagueId });

        Assert.Null(result);
    }

    [Fact]
    public async Task ColleaguesReturn_InTheViewableOrganization_IsFound()
    {
        var result = await Handle(new ReturnQuery { Id = ReturnId, CustomerId = ColleagueId, OrganizationId = "org-1" });

        Assert.Equal(ReturnId, result?.Id);
    }

    [Fact]
    public async Task ColleaguesReturn_InAnotherOrganization_IsNotFound()
    {
        var result = await Handle(new ReturnQuery { Id = ReturnId, CustomerId = ColleagueId, OrganizationId = "org-2" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ReturnWithoutOrganization_IsNotOpenedByTheOrganizationScope()
    {
        var result = await Handle(
            new ReturnQuery { Id = ReturnId, CustomerId = ColleagueId, OrganizationId = "org-1" },
            new Return { Id = ReturnId, CustomerId = OwnerId });

        Assert.Null(result);
    }

    [Fact]
    public async Task ColleaguesDraft_InTheViewableOrganization_IsNotFound()
    {
        var result = await Handle(
            new ReturnQuery { Id = ReturnId, CustomerId = ColleagueId, OrganizationId = "org-1" },
            new Return { Id = ReturnId, CustomerId = OwnerId, OrganizationId = "org-1", Status = ReturnStatus.Draft });

        Assert.Null(result);
    }

    [Fact]
    public async Task OwnDraft_IsFound()
    {
        var result = await Handle(
            new ReturnQuery { Id = ReturnId, CustomerId = OwnerId, OrganizationId = "org-1" },
            new Return { Id = ReturnId, CustomerId = OwnerId, OrganizationId = "org-1", Status = ReturnStatus.Draft });

        Assert.Equal(ReturnId, result?.Id);
    }

    [Fact]
    public async Task MissingReturn_IsNotFound()
    {
        var result = await Handle(new ReturnQuery { Id = "gone", CustomerId = OwnerId, OrganizationId = "org-1" });

        Assert.Null(result);
    }

    private static Task<Return> Handle(ReturnQuery query, Return stored = null)
    {
        stored ??= new Return { Id = ReturnId, CustomerId = OwnerId, OrganizationId = "org-1" };

        var returnService = new Mock<IReturnService>();
        returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
                ids.Contains(stored.Id) ? [stored] : new List<Return>());

        var flowService = new Mock<IReturnFlowService>();
        flowService
            .Setup(x => x.IsOwnedBy(It.IsAny<Return>(), It.IsAny<string>()))
            .ReturnsAsync((Return x, string customerId) => x.CustomerId == customerId);

        return new ReturnQueryHandler(returnService.Object, flowService.Object).Handle(query, CancellationToken.None);
    }
}
