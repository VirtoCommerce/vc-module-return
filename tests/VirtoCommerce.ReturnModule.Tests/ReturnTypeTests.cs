using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.Infrastructure;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnTypeTests
{
    [Fact]
    public async Task ColleaguesReturn_OffersNoAction()
    {
        // Read through the organization scope: every mutation would refuse the caller, so offering a
        // button would only lead to an error.
        var actions = await ResolveAvailableActions(isOwner: false);

        Assert.Equal(2, actions.Count);
        Assert.All(actions, x => Assert.False(x.IsAvailable));
        Assert.All(actions, x => Assert.Equal(ReturnFlowError.ReturnNotFound, x.UnavailableReason));
    }

    [Fact]
    public async Task OwnReturn_KeepsTheFlowsAnswer()
    {
        // The control: its own buyer gets each action exactly as the flow judged it.
        var actions = await ResolveAvailableActions(isOwner: true);

        Assert.True(actions.Single(x => x.Name == ReturnAction.Cancel).IsAvailable);
        Assert.Equal("WRONG_STATUS", actions.Single(x => x.Name == ReturnAction.Edit).UnavailableReason);
    }

    // Who the caller is reaches the resolver as a claim, but the platform's user-id claim types are
    // only configured at startup; ownership is therefore stood in for by the flow service.
    private static async Task<IList<ReturnFlowAction>> ResolveAvailableActions(bool isOwner)
    {
        var orderReturn = new Return { Id = "return-1", CustomerId = "buyer-1", OrganizationId = "org-1", Status = ReturnStatus.Requested };

        var flowService = new Mock<IReturnFlowService>();
        flowService
            .Setup(x => x.GetAvailableActions(orderReturn))
            .Returns(() =>
            [
                new ReturnFlowAction { Name = ReturnAction.Cancel, IsAvailable = true },
                new ReturnFlowAction { Name = ReturnAction.Edit, IsAvailable = false, UnavailableReason = "WRONG_STATUS" },
            ]);
        flowService
            .Setup(x => x.IsOwnedBy(orderReturn, It.IsAny<string>()))
            .ReturnsAsync(isOwner);

        var services = new ServiceCollection()
            .AddSingleton(flowService.Object)
            .BuildServiceProvider();

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "buyer-2")], "test"));

        // GraphQL.NET's resolver adapter reads the non-generic IResolveFieldContext.Source, so both views need the return.
        var context = new Mock<IResolveFieldContext<Return>>();
        context.SetupGet(x => x.Source).Returns(orderReturn);
        context.As<IResolveFieldContext>().SetupGet(x => x.Source).Returns(orderReturn);
        context.SetupGet(x => x.RequestServices).Returns(services);
        context.SetupGet(x => x.UserContext).Returns(new GraphQLUserContext(principal));

        var result = await new ReturnType().Fields.Find("availableActions").Resolver.ResolveAsync(context.Object);

        return ((IEnumerable<ReturnFlowAction>)result).ToList();
    }
}
