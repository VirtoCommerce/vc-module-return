using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.Xapi.Core.Services;
using Xunit;
using static VirtoCommerce.ReturnModule.Core.ModuleConstants.Security;
using CustomerModuleConstants = VirtoCommerce.CustomerModule.Core.ModuleConstants;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnAccessServiceTests
{
    private const string UserId = "user-1";
    private const string ContactId = "contact-1";
    private const string OrganizationId = "org-1";
    private const string OtherOrganizationId = "org-2";
    private const string ViewerRoleId = "role-viewer";
    private const string BuyerRoleId = "role-buyer";

    private readonly Mock<IUserManagerCore> _userManagerCore = new();
    private readonly Mock<IMemberService> _memberService = new();
    private readonly Mock<IOrganizationMembershipSearchService> _membershipSearchService = new();

    private readonly Mock<UserManager<ApplicationUser>> _userManager =
        new(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

    private readonly Mock<RoleManager<Role>> _roleManager =
        new(Mock.Of<IRoleStore<Role>>(), null, null, null, null);

    private readonly ApplicationUser _user = new() { Id = UserId, MemberId = ContactId, Roles = [] };
    private readonly Contact _contact = new() { Id = ContactId, Organizations = [OrganizationId, OtherOrganizationId] };
    private readonly Dictionary<string, OrganizationMembership> _memberships = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<OrganizationRole>> _organizationRoles = new(StringComparer.OrdinalIgnoreCase);

    public ReturnAccessServiceTests()
    {
        _userManager
            .Setup(x => x.FindByIdAsync(UserId))
            .ReturnsAsync(() => _user);

        _memberService
            .Setup(x => x.GetByIdAsync(ContactId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => _contact);

        _membershipSearchService
            .Setup(x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync((OrganizationMembershipSearchCriteria criteria, bool _) => new OrganizationMembershipSearchResult
            {
                Results = _memberships.TryGetValue(criteria.OrganizationId, out var membership) ? [membership] : [],
            });

        _membershipSearchService
            .Setup(x => x.GetRolesByUserAndOrgAsync(It.IsAny<string>(), It.IsAny<OrganizationMembership>()))
            .ReturnsAsync((string organizationId, OrganizationMembership _) =>
                _organizationRoles.TryGetValue(organizationId, out var roles) ? roles : []);

        _roleManager
            .Setup(x => x.FindByIdAsync(ViewerRoleId))
            .ReturnsAsync(NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView));

        _roleManager
            .Setup(x => x.FindByIdAsync(BuyerRoleId))
            .ReturnsAsync(NewRole(BuyerRoleId, "xapi:my_organization:order:view"));
    }

    [Fact]
    public async Task MemberHoldingThePermissionGlobally_CanView()
    {
        // A role assigned outside any organization, as the sample data's Organization maintainer is,
        // counts in every organization the user belongs to.
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];

        Assert.True(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task MemberHoldingThePermissionInTheOrganization_CanView()
    {
        _organizationRoles[OrganizationId] = [new OrganizationRole { OrganizationId = OrganizationId, RoleId = ViewerRoleId }];

        Assert.True(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task MemberHoldingThePermissionInAnotherOrganizationOnly_CannotView()
    {
        // Roles are per organization: a maintainer in one organization is a plain buyer in the other,
        // whichever organization their token was issued for.
        _organizationRoles[OrganizationId] = [new OrganizationRole { OrganizationId = OrganizationId, RoleId = BuyerRoleId }];
        _organizationRoles[OtherOrganizationId] = [new OrganizationRole { OrganizationId = OtherOrganizationId, RoleId = ViewerRoleId }];

        var service = CreateService();

        Assert.False(await service.CanViewOrganizationAsync(UserId, OrganizationId));
        Assert.True(await service.CanViewOrganizationAsync(UserId, OtherOrganizationId));
    }

    [Fact]
    public async Task MemberWithoutThePermission_CannotView()
    {
        _user.Roles = [NewRole(BuyerRoleId, "xapi:my_organization:order:view")];
        _organizationRoles[OrganizationId] = [new OrganizationRole { OrganizationId = OrganizationId, RoleId = BuyerRoleId }];

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task OrganizationInOtherCase_CanView()
    {
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];

        Assert.True(await CreateService().CanViewOrganizationAsync(UserId, "ORG-1"));
    }

    [Fact]
    public async Task PermissionWithoutMembership_CannotView()
    {
        // The permission says what a member may do, not which organizations they may look into.
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];
        _contact.Organizations = [OtherOrganizationId];

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task AdministratorWhoIsAMember_CanView()
    {
        _user.IsAdministrator = true;

        Assert.True(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task AdministratorOutsideTheOrganization_CannotView()
    {
        // No bypass: an administrator holds every permission but still has to belong to the organization.
        _user.IsAdministrator = true;
        _contact.Organizations = [OtherOrganizationId];

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task LockedMembership_CannotView()
    {
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];
        _memberships[OrganizationId] = new OrganizationMembership { UserId = UserId, OrganizationId = OrganizationId, IsLocked = true };

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task LockThatHasEnded_CanView()
    {
        // The control for the lock: one whose end has passed no longer holds.
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];
        _memberships[OrganizationId] = new OrganizationMembership
        {
            UserId = UserId,
            OrganizationId = OrganizationId,
            IsLocked = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(-1),
        };

        Assert.True(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Theory]
    [InlineData(CustomerModuleConstants.MembershipStatuses.Invited)]
    [InlineData(CustomerModuleConstants.MembershipStatuses.Rejected)]
    [InlineData(CustomerModuleConstants.MembershipStatuses.Deleted)]
    public async Task MembershipThatIsNotActive_CannotView(string status)
    {
        // An invitation adds the organization to the contact before it is accepted, so the membership's
        // status is what keeps an invitee out: the organization id alone would let them in.
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];
        _memberships[OrganizationId] = new OrganizationMembership { UserId = UserId, OrganizationId = OrganizationId, Status = status };

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task ApprovedMembership_CanView()
    {
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];
        _memberships[OrganizationId] = new OrganizationMembership
        {
            UserId = UserId,
            OrganizationId = OrganizationId,
            Status = CustomerModuleConstants.MembershipStatuses.Approved,
        };

        Assert.True(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Fact]
    public async Task ContactStatus_CountsWhenThereIsNoMembershipRecord()
    {
        // Contacts added to an organization before memberships were recorded carry their status on
        // the contact, and Customer reads it from there.
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];
        _contact.Status = CustomerModuleConstants.MembershipStatuses.Rejected;

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task NoOrganization_CannotView(string organizationId)
    {
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, organizationId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-user")]
    public async Task NoUser_CannotView(string userId)
    {
        Assert.False(await CreateService().CanViewOrganizationAsync(userId, OrganizationId));
    }

    [Fact]
    public async Task UserWithoutContact_CannotView()
    {
        _user.MemberId = null;
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];

        Assert.False(await CreateService().CanViewOrganizationAsync(UserId, OrganizationId));
    }

    [Theory]
    [InlineData(ReturnStatus.Requested)]
    [InlineData(ReturnStatus.Cancelled)] // withdrawn after its submit: still the organization's
    public async Task SubmittedReturnOfAViewableOrganization_IsVisible(string status)
    {
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];

        Assert.True(await CreateService().CanViewReturnAsync(UserId, NewReturn(status, OrganizationId)));
    }

    [Theory]
    [InlineData(ReturnStatus.Draft)]
    [InlineData(ReturnStatus.Cancelled)] // a draft its buyer cancelled before submitting it (VCST-6226)
    public async Task Draft_IsNotVisible(string status)
    {
        // Not through the organization, whoever reads it: its own buyer reaches it as the owner.
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];

        Assert.False(await CreateService().CanViewReturnAsync(UserId, NewReturn(status, OrganizationId, submitted: false)));
    }

    [Fact]
    public async Task ReturnOfAnOrganizationTheUserCannotView_IsNotVisible()
    {
        // The return's own organization decides, not the one the caller happens to have selected.
        _organizationRoles[OrganizationId] = [new OrganizationRole { OrganizationId = OrganizationId, RoleId = ViewerRoleId }];

        Assert.False(await CreateService().CanViewReturnAsync(UserId, NewReturn(ReturnStatus.Requested, OtherOrganizationId)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ReturnWithoutOrganization_IsNotVisible(string organizationId)
    {
        _user.Roles = [NewRole(ViewerRoleId, XapiPermissions.MyOrganizationReturnView)];

        Assert.False(await CreateService().CanViewReturnAsync(UserId, NewReturn(ReturnStatus.Requested, organizationId)));
    }

    [Fact]
    public async Task AnonymousCaller_IsRefusedBeforeAnyLookup()
    {
        var context = CreateContext(authenticated: false);

        var error = await Assert.ThrowsAsync<AuthorizationError>(() => CreateService().CheckUserStateAsync(context));

        Assert.Equal(AuthorizationError.AnonymousAccessDenied().Code, error.Code);
        _userManagerCore.Verify(x => x.CheckCurrentUserState(It.IsAny<IResolveFieldContext>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task SignedInCaller_IsCheckedByXapi()
    {
        // X-API decides what an unusable account is: missing, locked, or with its password expired.
        var context = CreateContext(authenticated: true);
        _userManagerCore
            .Setup(x => x.CheckCurrentUserState(context, false))
            .ThrowsAsync(AuthorizationError.UserLocked());

        var error = await Assert.ThrowsAsync<AuthorizationError>(() => CreateService().CheckUserStateAsync(context));

        Assert.Equal(AuthorizationError.UserLocked().Code, error.Code);
    }

    private ReturnAccessService CreateService()
    {
        return new ReturnAccessService(
            _userManagerCore.Object,
            _memberService.Object,
            _membershipSearchService.Object,
            () => _userManager.Object,
            () => _roleManager.Object);
    }

    private static Role NewRole(string id, string permission)
    {
        return new Role { Id = id, Name = id, Permissions = [new Permission { Name = permission }] };
    }

    private static Return NewReturn(string status, string organizationId, bool submitted = true)
    {
        return new Return
        {
            Id = "return-1",
            CustomerId = "user-2",
            OrganizationId = organizationId,
            Status = status,
            SubmittedDate = submitted ? new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc) : null,
        };
    }

    private static IResolveFieldContext CreateContext(bool authenticated)
    {
        // An identity with no authentication type reads as anonymous.
        var identity = authenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)], "test")
            : new ClaimsIdentity();

        var context = new Mock<IResolveFieldContext>();
        context.SetupGet(x => x.UserContext).Returns(new GraphQLUserContext(new ClaimsPrincipal(identity)));

        return context.Object;
    }
}
