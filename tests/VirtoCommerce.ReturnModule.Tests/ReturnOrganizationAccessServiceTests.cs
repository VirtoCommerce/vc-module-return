using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using Xunit;
using static VirtoCommerce.ReturnModule.Core.ModuleConstants.Security;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnOrganizationAccessServiceTests
{
    private const string UserId = "user-1";
    private const string ContactId = "contact-1";
    private const string OrganizationId = "org-1";

    private readonly Mock<UserManager<ApplicationUser>> _userManager =
        new(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

    private readonly Mock<IMemberService> _memberService = new();

    public ReturnOrganizationAccessServiceTests()
    {
        _userManager
            .Setup(x => x.FindByIdAsync(UserId))
            .ReturnsAsync(new ApplicationUser { Id = UserId, MemberId = ContactId });

        _memberService
            .Setup(x => x.GetByIdAsync(ContactId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Contact { Id = ContactId, Organizations = [OrganizationId] });
    }

    [Fact]
    public async Task MemberWithPermission_CanView()
    {
        Assert.True(await CreateService().CanViewAsync(Principal(withPermission: true), OrganizationId));
    }

    [Fact]
    public async Task MemberWithPermission_OrganizationInOtherCase_CanView()
    {
        Assert.True(await CreateService().CanViewAsync(Principal(withPermission: true), "ORG-1"));
    }

    [Fact]
    public async Task MemberWithoutPermission_CannotView()
    {
        Assert.False(await CreateService().CanViewAsync(Principal(withPermission: false), OrganizationId));
    }

    [Fact]
    public async Task PermissionForAnotherOrganization_CannotView()
    {
        Assert.False(await CreateService().CanViewAsync(Principal(withPermission: true), "org-2"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task NoOrganization_CannotView(string organizationId)
    {
        Assert.False(await CreateService().CanViewAsync(Principal(withPermission: true), organizationId));
    }

    [Fact]
    public async Task UserWithoutContact_CannotView()
    {
        _userManager
            .Setup(x => x.FindByIdAsync(UserId))
            .ReturnsAsync(new ApplicationUser { Id = UserId });

        Assert.False(await CreateService().CanViewAsync(Principal(withPermission: true), OrganizationId));
    }

    [Fact]
    public async Task AdministratorOutsideTheOrganization_CannotView()
    {
        _memberService
            .Setup(x => x.GetByIdAsync(ContactId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Contact { Id = ContactId, Organizations = ["org-2"] });

        var principal = Principal(withPermission: false, role: PlatformConstants.Security.SystemRoles.Administrator);

        Assert.False(await CreateService().CanViewAsync(principal, OrganizationId));
    }

    [Fact]
    public void SubmittedReturnOfTheOrganization_IsVisible()
    {
        Assert.True(CreateService().IsVisibleToOrganization(NewReturn(ReturnStatus.Requested), OrganizationId));
    }

    [Fact]
    public void ReturnOfTheOrganization_OrganizationInOtherCase_IsVisible()
    {
        Assert.True(CreateService().IsVisibleToOrganization(NewReturn(ReturnStatus.Requested), "ORG-1"));
    }

    [Fact]
    public void Draft_IsNotVisible()
    {
        // Not through the organization, whoever reads it: its own buyer reaches it as the owner.
        Assert.False(CreateService().IsVisibleToOrganization(NewReturn(ReturnStatus.Draft), OrganizationId));
    }

    [Fact]
    public void AnotherOrganizationsReturn_IsNotVisible()
    {
        Assert.False(CreateService().IsVisibleToOrganization(NewReturn(ReturnStatus.Requested), "org-2"));
    }

    [Fact]
    public void ReturnWithoutOrganization_IsNotVisible()
    {
        var orderReturn = NewReturn(ReturnStatus.Requested);
        orderReturn.OrganizationId = null;

        Assert.False(CreateService().IsVisibleToOrganization(orderReturn, OrganizationId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoOrganizationOnEitherSide_IsNotVisible(string organizationId)
    {
        // A caller with no organization selected must not match every return that has none.
        var orderReturn = NewReturn(ReturnStatus.Requested);
        orderReturn.OrganizationId = organizationId;

        Assert.False(CreateService().IsVisibleToOrganization(orderReturn, organizationId));
    }

    private static Return NewReturn(string status)
    {
        return new Return { Id = "return-1", CustomerId = "user-2", OrganizationId = OrganizationId, Status = status };
    }

    private ReturnOrganizationAccessService CreateService()
    {
        return new TestService(_memberService.Object, () => _userManager.Object);
    }

    // The platform maps user id claim types at startup, so outside it GetUserId finds nothing.
    private sealed class TestService : ReturnOrganizationAccessService
    {
        public TestService(IMemberService memberService, Func<UserManager<ApplicationUser>> userManagerFactory)
            : base(memberService, userManagerFactory)
        {
        }

        protected override string GetUserId(ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static ClaimsPrincipal Principal(bool withPermission, string role = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, UserId) };

        if (withPermission)
        {
            claims.Add(new Claim(PlatformConstants.Security.Claims.PermissionClaimType, XapiPermissions.MyOrganizationReturnView));
        }

        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", ClaimTypes.Role));
    }
}
