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
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.Xapi.Core.Services;
using Xunit;
using CustomerClaims = VirtoCommerce.CustomerModule.Core.ModuleConstants.Security.Claims;
using FilePermissions = VirtoCommerce.FileExperienceApi.Core.ModuleConstants.Security.Permissions;

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
    public async Task BackOfficeReaderOpeningAFile_Succeeds()
    {
        // The agent decides on the return from its photos, so whoever may read returns may open them.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read, userPermission: ModuleConstants.Security.Permissions.Read);

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task BackOfficeReaderDeletingAFile_Fails()
    {
        // Reading returns is not a licence to destroy the buyer's evidence.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Delete, userPermission: ModuleConstants.Security.Permissions.Read);

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task SignedInUserWithoutReturnReadOpeningAFile_Fails()
    {
        // The control for the back-office tests above and the organization tests below: being signed in
        // opens nothing, the permission or the organization's rule does.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read);

        await CreateHandler(OtherId).HandleAsync(context);

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

    [Theory]
    [InlineData(ReturnStatus.Draft)]
    [InlineData(ReturnStatus.Cancelled)] // a draft its buyer cancelled before submitting it (VCST-6226)
    public async Task OrganizationViewerReadingAColleaguesDraftFile_Fails(string status)
    {
        // A colleague's draft does not open through the organization, so neither do its photos.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read);

        await CreateHandler(OtherId, organizationViewer: true, status: status, submitted: false).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData("org-2")]
    [InlineData(null)]
    public async Task OrganizationViewer_FollowsTheReturnsOrganization_NotTheSelectedOne(string selectedOrganizationId)
    {
        // A contact of two organizations who may read the return's one opens its photos whichever
        // organization they have selected, as the return itself opens and as the organization list
        // takes any of their organizations.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Read, selectedOrganizationId: selectedOrganizationId);

        await CreateHandler(OtherId, organizationViewer: true).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task BackOfficeReaderOpeningAFileOfAMissingReturn_Fails()
    {
        // The permission opens a return's photos, not whatever file claims to belong to one.
        var file = new File { Id = "f1", OwnerEntityType = nameof(Return), OwnerEntityId = "gone" };
        var context = CreateContext(OtherId, file, permission: FilePermissions.Read, userPermission: ModuleConstants.Security.Permissions.Read);

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task BackOfficeReaderOpeningAFileOwnedBySomethingElse_Fails()
    {
        var file = new File { Id = "f1", OwnerEntityType = "Quote", OwnerEntityId = ReturnId };
        var context = CreateContext(OtherId, file, permission: FilePermissions.Read, userPermission: ModuleConstants.Security.Permissions.Read);

        await CreateHandler(OtherId).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandlerOverridingTheReleasedCheck_IsStillConsulted()
    {
        // 3.1002.0 shipped IsAllowedAsync(context) as the seam: a handler built on it has to keep working,
        // here one that lets a colleague delete a buyer's file.
        var context = CreateContext(OtherId, OwnedFile(), permission: FilePermissions.Delete);

        await new ReleasedSeamHandler(DefaultScopeFactory()).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task OwnerDeletingOwnFile_Succeeds()
    {
        var context = CreateContext(OwnerId, OwnedFile(), permission: FilePermissions.Delete);

        await CreateHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void RequirementFactory_CarriesWhatTheCallerWantsToDo()
    {
        // The factory is the only thing that tells the handler a read from a delete.
        var requirement = new ReturnFileAuthorizationRequirementFactory().Create(OwnedFile(), FilePermissions.Delete);

        Assert.Equal(FilePermissions.Delete, Assert.IsType<ReturnAuthorizationRequirement>(requirement).Permission);
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

    private sealed class ReleasedSeamHandler : ReturnAuthorizationHandler
    {
        public ReleasedSeamHandler(IServiceScopeFactory scopeFactory)
            : base(scopeFactory)
        {
        }

        protected override Task<bool> IsAllowedAsync(AuthorizationHandlerContext context) => Task.FromResult(true);
    }

    private static ReturnAuthorizationHandler CreateHandler(string userId = OwnerId, bool organizationViewer = false, string status = ReturnStatus.Requested, bool submitted = true)
    {
        return new TestHandler(DefaultScopeFactory(organizationViewer, status, submitted), userId);
    }

    private static IServiceScopeFactory DefaultScopeFactory(bool organizationViewer = false, string status = ReturnStatus.Requested, bool submitted = true)
    {
        var orderReturn = new Return
        {
            Id = ReturnId,
            CustomerId = OwnerId,
            OrganizationId = "org-1",
            Status = status,
            SubmittedDate = submitted ? new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc) : null,
        };

        var returnService = new Mock<IReturnService>();
        returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
                ids.Contains(ReturnId) ? [orderReturn] : new List<Return>());

        var flowService = new Mock<IReturnFlowService>();
        flowService
            .Setup(x => x.IsOwnedBy(It.IsAny<Return>(), It.IsAny<string>()))
            .ReturnsAsync((Return x, string customerId) => x.CustomerId == customerId);

        // The real submit rule; only the organization rule is stood in for, and it answers for the
        // caller and the return's own organization only, so the handler has to ask about exactly those.
        var accessService = new Mock<ReturnAccessService>(
            Mock.Of<IUserManagerCore>(),
            Mock.Of<IMemberService>(),
            Mock.Of<IOrganizationMembershipSearchService>(),
            (Func<UserManager<ApplicationUser>>)(() => null),
            (Func<RoleManager<Role>>)(() => null))
        {
            CallBase = true,
        };
        accessService
            .Setup(x => x.CanViewOrganizationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        accessService
            .Setup(x => x.CanViewOrganizationAsync(OtherId, "org-1"))
            .ReturnsAsync(organizationViewer);

        return ScopeFactoryFor(returnService.Object, flowService.Object, accessService.Object);
    }

    // The handler is a singleton and resolves the return services per check, so the test has to
    // hand it a scope rather than the services themselves.
    private static IServiceScopeFactory ScopeFactoryFor(
        IReturnService returnService,
        IReturnFlowService flowService,
        IReturnAccessService accessService)
    {
        var provider = new ServiceCollection()
            .AddSingleton(returnService)
            .AddSingleton(flowService)
            .AddSingleton(accessService)
            .BuildServiceProvider();

        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private static AuthorizationHandlerContext CreateContext(
        string userId,
        object resource,
        string role = null,
        bool authenticated = true,
        string permission = null,
        string selectedOrganizationId = "org-1",
        string userPermission = null)
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

        if (userPermission != null)
        {
            claims.Add(new Claim(PlatformConstants.Security.Claims.PermissionClaimType, userPermission));
        }

        // An identity with no authentication type reads as anonymous.
        var identity = authenticated
            ? new ClaimsIdentity(claims, "test", "name", ClaimTypes.Role)
            : new ClaimsIdentity(claims, null, "name", ClaimTypes.Role);

        var user = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext([new ReturnAuthorizationRequirement { Permission = permission }], user, resource);
    }
}
