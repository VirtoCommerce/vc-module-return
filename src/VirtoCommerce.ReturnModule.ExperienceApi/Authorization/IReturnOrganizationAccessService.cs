using System.Security.Claims;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public interface IReturnOrganizationAccessService
{
    /// <summary>
    /// Whether the caller may read returns raised by colleagues of the given organization. Never
    /// grants a change: every mutation stays with the buyer who raised the return.
    /// </summary>
    Task<bool> CanViewAsync(ClaimsPrincipal principal, string organizationId);

    bool IsVisibleToOrganization(Return orderReturn, string organizationId);
}
