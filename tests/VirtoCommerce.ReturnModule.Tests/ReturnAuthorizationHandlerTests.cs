using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Models;
using CustomerClaims = VirtoCommerce.CustomerModule.Core.ModuleConstants.Security.Claims;
using FilePermissions = VirtoCommerce.FileExperienceApi.Core.ModuleConstants.Security.Permissions;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
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

    [Fact]
    public async Task OrganizationViewerReadingAColleaguesFile_Succeeds()
    {
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read);

        await CreateHandler(OtherId, organizationViewer: true).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task OrganizationViewerDeletingAColleaguesFile_Fails()
    {
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Delete);

        await CreateHandler(OtherId, organizationViewer: true).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task ColleagueWithoutOrganizationAccessReadingAFile_Fails()
    {
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read);

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task OrganizationViewerReadingAColleaguesDraftFile_Fails()
    {
        // A colleague's draft does not open through the organization, so neither do its photos.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read);

        await CreateHandler(OtherId, organizationViewer: true, status: ReturnStatus.Draft).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task OrganizationViewerSwitchedToAnotherOrganization_Fails()
    {
        // A contact of two organizations, allowed to read both, who has switched to the other one:
        // the photos follow the organization the list shows, as the return itself does.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read, selectedOrganizationId: "org-2");

        await CreateHandler(OtherId, organizationViewer: true).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task OrganizationViewerWithNoOrganizationSelected_Fails()
    {
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read, selectedOrganizationId: null);

        await CreateHandler(OtherId, organizationViewer: true).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task OwnerDeletingOwnFile_Succeeds()
    {
        var context = CreateContext(OwnerId, OwnedFile(), permission: FilePermissions.Delete);

        await CreateHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
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

        public TestHandler(IServiceScopeFactory scopeFactory, string userId)
            : base(scopeFactory)
        {
            _userId = userId;
        }

        protected override string GetUserId(AuthorizationHandlerContext context) => _userId;
    }

    private static ReturnAuthorizationHandler CreateHandler(string userId = OwnerId, bool organizationViewer = false, string status = ReturnStatus.Requested)
    {
        var orderReturn = new Return { Id = ReturnId, CustomerId = OwnerId, OrganizationId = "org-1", Status = status };

        var returnService = new Mock<IReturnService>();
        returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
                ids.Contains(ReturnId) ? [orderReturn] : new List<Return>());

        var flowService = new Mock<IReturnFlowService>();
        flowService
            .Setup(x => x.IsOwnedBy(It.IsAny<Return>(), It.IsAny<string>()))
            .ReturnsAsync((Return x, string customerId) => x.CustomerId == customerId);

        // The real visibility rule; only the membership lookup is stood in for, and it answers the
        // same for every organization so that the organization compared is the one selected.
        var organizationAccessService = new Mock<ReturnOrganizationAccessService>(
            Mock.Of<IMemberService>(),
            (Func<UserManager<ApplicationUser>>)(() => null))
        {
            CallBase = true,
        };
        organizationAccessService
            .Setup(x => x.CanViewAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .ReturnsAsync(organizationViewer);

        return new TestHandler(ScopeFactoryFor(returnService.Object, flowService.Object, organizationAccessService.Object), userId);
    }

    // The handler is a singleton and resolves the return services per check, so the test has to
    // hand it a scope rather than the services themselves.
    private static IServiceScopeFactory ScopeFactoryFor(
        IReturnService returnService,
        IReturnFlowService flowService,
        IReturnOrganizationAccessService organizationAccessService)
    {
        var provider = new ServiceCollection()
            .AddSingleton(returnService)
            .AddSingleton(flowService)
            .AddSingleton(organizationAccessService)
            .BuildServiceProvider();

        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private static AuthorizationHandlerContext CreateContext(
        string userId,
        object resource,
        string role = null,
        bool authenticated = true,
        string permission = null,
        string selectedOrganizationId = "org-1")
    {
        var claims = new List<Claim> { new("name", userId), new(ClaimTypes.NameIdentifier, userId) };

        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (selectedOrganizationId != null)
        {
            claims.Add(new Claim(CustomerClaims.OrganizationId, selectedOrganizationId));
        }

        // An identity with no authentication type reads as anonymous.
        var identity = authenticated
            ? new ClaimsIdentity(claims, "test", "name", ClaimTypes.Role)
            : new ClaimsIdentity(claims, null, "name", ClaimTypes.Role);

        var user = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext([new ReturnAuthorizationRequirement { Permission = permission }], user, resource);
    }
}
