using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Moq;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnAuthorizationHandlerTests
{
    private const string OwnerId = "buyer-1";
    private const string OtherId = "buyer-2";
    private const string ReturnId = "return-1";

    [Fact]
    public async Task OwnFile_Succeeds()
    {
        var context = CreateContext(OwnerId, OwnedFile());

        await CreateHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task AnotherBuyersFile_Fails()
    {
        var context = CreateContext(OtherId, OwnedFile());

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task UnclaimedFile_Succeeds()
    {
        var file = new File { Id = "f1", Scope = "return-attachments" };
        var context = CreateContext(OtherId, file);

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task FileOwnedBySomethingElse_Fails()
    {
        var file = new File { Id = "f1", OwnerEntityType = "Quote", OwnerEntityId = ReturnId };
        var context = CreateContext(OwnerId, file);

        await CreateHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task MissingReturn_Fails()
    {
        var file = new File { Id = "f1", OwnerEntityType = nameof(Return), OwnerEntityId = "gone" };
        var context = CreateContext(OwnerId, file);

        await CreateHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Administrator_Succeeds()
    {
        var context = CreateContext(OtherId, OwnedFile(), PlatformConstants.Security.SystemRoles.Administrator);

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ResourceThatIsNotAFile_Fails()
    {
        var context = CreateContext(OwnerId, resource: "something");

        await CreateHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task AnonymousCallerOnUnclaimedFile_Fails()
    {
        var file = new File { Id = "f1", Scope = "return-attachments" };
        var context = CreateContext(OwnerId, file, authenticated: false);

        await CreateHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task AnonymousCallerOnOwnedFile_Fails()
    {
        var context = CreateContext(OwnerId, OwnedFile(), authenticated: false);

        await CreateHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static File OwnedFile()
    {
        return new File
        {
            Id = "f1",
            Scope = "return-attachments",
            OwnerEntityType = nameof(Return),
            OwnerEntityId = ReturnId,
        };
    }

    private sealed class TestHandler : ReturnAuthorizationHandler
    {
        private readonly string _userId;

        public TestHandler(IReturnService returnService, IReturnFlowService flowService, string userId)
            : base(returnService, flowService)
        {
            _userId = userId;
        }

        protected override string GetUserId(AuthorizationHandlerContext context) => _userId;
    }

    private static ReturnAuthorizationHandler CreateHandler(string userId = OwnerId)
    {
        var orderReturn = new Return { Id = ReturnId, CustomerId = OwnerId };

        var returnService = new Mock<IReturnService>();
        returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
                ids.Contains(ReturnId) ? [orderReturn] : new List<Return>());

        var flowService = new Mock<IReturnFlowService>();
        flowService
            .Setup(x => x.IsOwnedByAsync(It.IsAny<Return>(), It.IsAny<string>()))
            .ReturnsAsync((Return x, string customerId) => x.CustomerId == customerId);

        return new TestHandler(returnService.Object, flowService.Object, userId);
    }

    private static AuthorizationHandlerContext CreateContext(
        string userId,
        object resource,
        string role = null,
        bool authenticated = true)
    {
        var claims = new List<Claim> { new("name", userId), new(ClaimTypes.NameIdentifier, userId) };

        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // An identity with no authentication type reads as anonymous.
        var identity = authenticated
            ? new ClaimsIdentity(claims, "test", "name", ClaimTypes.Role)
            : new ClaimsIdentity(claims, null, "name", ClaimTypes.Role);

        var user = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext([new ReturnAuthorizationRequirement()], user, resource);
    }
}
