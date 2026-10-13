using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Queries;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnQueryHandlerTests
{
    private const string ReturnId = "return-1";
    private const string OwnerId = "buyer-1";
    private const string ColleagueId = "buyer-2";

    private readonly Mock<IReturnAccessService> _accessService = new();

    [Fact]
    public async Task OwnReturn_IsFound()
    {
        var result = await Handle(new ReturnQuery { Id = ReturnId, CustomerId = OwnerId });

        Assert.Equal(ReturnId, result?.Id);
        // Its own buyer never needs the organization's permission.
        _accessService.Verify(x => x.CanViewReturnAsync(It.IsAny<string>(), It.IsAny<Return>()), Times.Never);
    }

    [Fact]
    public async Task OwnDraft_IsFound()
    {
        var result = await Handle(
            new ReturnQuery { Id = ReturnId, CustomerId = OwnerId },
            new Return { Id = ReturnId, CustomerId = OwnerId, OrganizationId = "org-1", Status = ReturnStatus.Draft });

        Assert.Equal(ReturnId, result?.Id);
    }

    [Fact]
    public async Task ColleaguesReturn_TheOrganizationRuleAllows_IsFound()
    {
        // The rule is asked about the caller and the return as stored, so it judges the return's own
        // organization rather than one the caller names.
        _accessService
            .Setup(x => x.CanViewReturnAsync(ColleagueId, It.Is<Return>(r => r.Id == ReturnId && r.OrganizationId == "org-1")))
            .ReturnsAsync(true);

        var result = await Handle(new ReturnQuery { Id = ReturnId, CustomerId = ColleagueId });

        Assert.Equal(ReturnId, result?.Id);
    }

    [Fact]
    public async Task ColleaguesReturn_TheOrganizationRuleRefuses_IsNotFound()
    {
        // The same answer an unknown id gets, so a refusal tells nobody the return exists.
        _accessService
            .Setup(x => x.CanViewReturnAsync(It.IsAny<string>(), It.IsAny<Return>()))
            .ReturnsAsync(false);

        var result = await Handle(new ReturnQuery { Id = ReturnId, CustomerId = ColleagueId });

        Assert.Null(result);
    }

    [Fact]
    public async Task MissingReturn_IsNotFound()
    {
        var result = await Handle(new ReturnQuery { Id = "gone", CustomerId = OwnerId });

        Assert.Null(result);
        _accessService.Verify(x => x.CanViewReturnAsync(It.IsAny<string>(), It.IsAny<Return>()), Times.Never);
    }

    private Task<Return> Handle(ReturnQuery query, Return stored = null)
    {
        stored ??= new Return { Id = ReturnId, CustomerId = OwnerId, OrganizationId = "org-1", Status = ReturnStatus.Requested };

        var returnService = new Mock<IReturnService>();
        returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
                ids.Contains(stored.Id) ? [stored] : new List<Return>());

        var flowService = new Mock<IReturnFlowService>();
        flowService
            .Setup(x => x.IsOwnedBy(It.IsAny<Return>(), It.IsAny<string>()))
            .ReturnsAsync((Return x, string customerId) => x.CustomerId == customerId);

        return new ReturnQueryHandler(returnService.Object, flowService.Object, _accessService.Object).Handle(query, CancellationToken.None);
    }
}
