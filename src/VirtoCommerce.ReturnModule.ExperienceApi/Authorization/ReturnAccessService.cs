using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.Xapi.Core.Services;
using static VirtoCommerce.ReturnModule.Core.ModuleConstants.Security;
using CustomerModuleConstants = VirtoCommerce.CustomerModule.Core.ModuleConstants;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public class ReturnAccessService : IReturnAccessService
{
    private readonly IUserManagerCore _userManagerCore;
    private readonly IMemberService _memberService;
    private readonly IOrganizationMembershipSearchService _membershipSearchService;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
    private readonly Func<RoleManager<Role>> _roleManagerFactory;

    public ReturnAccessService(
        IUserManagerCore userManagerCore,
        IMemberService memberService,
        IOrganizationMembershipSearchService membershipSearchService,
        Func<UserManager<ApplicationUser>> userManagerFactory,
        Func<RoleManager<Role>> roleManagerFactory)
    {
        _userManagerCore = userManagerCore;
        _memberService = memberService;
        _membershipSearchService = membershipSearchService;
        _userManagerFactory = userManagerFactory;
        _roleManagerFactory = roleManagerFactory;
    }

    public virtual async Task CheckUserStateAsync(IResolveFieldContext context)
    {
        // CheckCurrentUserState refuses an anonymous caller too, but only after a user lookup.
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        await _userManagerCore.CheckCurrentUserState(context, allowAnonymous: false);
    }

    public virtual async Task<bool> CanViewOrganizationAsync(string userId, string organizationId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(organizationId))
        {
            return false;
        }

        var user = await GetUserAsync(userId);
        var member = await GetMemberAsync(user);

        if (!IsMember(member, organizationId))
        {
            return false;
        }

        var membership = await _membershipSearchService.GetMembershipAsync(user.Id, organizationId);

        return IsActive(membership, member) &&
            await HasPermissionAsync(user, organizationId, membership, XapiPermissions.MyOrganizationReturnView);
    }

    public virtual async Task<bool> CanViewReturnAsync(string userId, Return orderReturn)
    {
        // Only a submitted return is the organization's: a draft, cancelled or not, stays with its buyer.
        return orderReturn?.SubmittedDate != null &&
            await CanViewOrganizationAsync(userId, orderReturn.OrganizationId);
    }

    protected virtual async Task<ApplicationUser> GetUserAsync(string userId)
    {
        using var userManager = _userManagerFactory();

        return await userManager.FindByIdAsync(userId);
    }

    protected virtual async Task<Member> GetMemberAsync(ApplicationUser user)
    {
        return string.IsNullOrEmpty(user?.MemberId) ? null : await _memberService.GetByIdAsync(user.MemberId);
    }

    protected virtual bool IsMember(Member member, string organizationId)
    {
        return (member as IHasOrganizations)?.Organizations?.Contains(organizationId, StringComparer.OrdinalIgnoreCase) == true;
    }

    // The rule Customer applies whenever it issues a token for an organization.
    protected virtual bool IsActive(OrganizationMembership membership, Member member)
    {
        if (membership?.IsCurrentlyLocked == true)
        {
            return false;
        }

        var status = OrganizationMembership.ResolveEffectiveStatus(membership?.Status, member.Status);

        return !CustomerModuleConstants.MembershipStatuses.IsBlocking(status);
    }

    // The token carries permissions for the selected organization only, so any organization is worked out
    // the way Customer builds them: the user's global roles plus their roles in that organization.
    protected virtual async Task<bool> HasPermissionAsync(ApplicationUser user, string organizationId, OrganizationMembership membership, string permission)
    {
        if (user.IsAdministrator || user.Roles?.Any(x => HasPermission(x, permission)) == true)
        {
            return true;
        }

        var organizationRoles = await _membershipSearchService.GetRolesByUserAndOrgAsync(organizationId, membership);

        if (organizationRoles.Count == 0)
        {
            return false;
        }

        using var roleManager = _roleManagerFactory();

        foreach (var organizationRole in organizationRoles)
        {
            if (HasPermission(await roleManager.FindByIdAsync(organizationRole.RoleId), permission))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPermission(Role role, string permission)
    {
        return role?.Permissions?.Any(x => x.Name.EqualsIgnoreCase(permission)) == true;
    }
}
