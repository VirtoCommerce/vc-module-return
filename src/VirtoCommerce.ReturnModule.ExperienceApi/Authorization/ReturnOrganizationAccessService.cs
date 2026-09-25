using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using static VirtoCommerce.ReturnModule.Core.ModuleConstants.Security;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public class ReturnOrganizationAccessService : IReturnOrganizationAccessService
{
    private readonly IMemberService _memberService;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public ReturnOrganizationAccessService(IMemberService memberService, Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        _memberService = memberService;
        _userManagerFactory = userManagerFactory;
    }

    public virtual async Task<bool> CanViewAsync(ClaimsPrincipal principal, string organizationId)
    {
        if (string.IsNullOrEmpty(organizationId) ||
            principal?.HasGlobalPermission(XapiPermissions.MyOrganizationReturnView) != true)
        {
            return false;
        }

        // The permission comes with a role, and a role is not tied to one organization: a contact
        // holding it still sees only the organizations they belong to.
        var member = await GetMemberAsync(GetUserId(principal));

        return member switch
        {
            Contact contact => contact.Organizations?.Contains(organizationId, StringComparer.OrdinalIgnoreCase) == true,
            Employee employee => employee.Organizations?.Contains(organizationId, StringComparer.OrdinalIgnoreCase) == true,
            _ => false,
        };
    }

    public virtual bool IsVisibleToOrganization(Return orderReturn, string organizationId)
    {
        // A draft is the buyer's own work in progress, not yet something the organization is waiting on.
        return !string.IsNullOrEmpty(organizationId) &&
            orderReturn.OrganizationId.EqualsIgnoreCase(organizationId) &&
            !orderReturn.Status.EqualsIgnoreCase(ReturnStatus.Draft);
    }

    protected virtual string GetUserId(ClaimsPrincipal principal)
    {
        return principal.GetUserId();
    }

    protected virtual async Task<Member> GetMemberAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        using var userManager = _userManagerFactory();
        var user = await userManager.FindByIdAsync(userId);

        return string.IsNullOrEmpty(user?.MemberId) ? null : await _memberService.GetByIdAsync(user.MemberId);
    }
}
