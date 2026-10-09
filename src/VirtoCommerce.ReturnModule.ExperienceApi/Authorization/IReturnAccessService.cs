using System.Threading.Tasks;
using GraphQL;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public interface IReturnAccessService
{
    Task CheckUserStateAsync(IResolveFieldContext context);

    // Read access only: every change stays with the buyer who raised the return.
    Task<bool> CanViewOrganizationAsync(string userId, string organizationId);

    Task<bool> CanViewReturnAsync(string userId, Return orderReturn);
}
