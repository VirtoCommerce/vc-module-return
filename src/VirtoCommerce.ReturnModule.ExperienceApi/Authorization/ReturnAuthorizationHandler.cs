using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using FileExperienceApiModuleConstants = VirtoCommerce.FileExperienceApi.Core.ModuleConstants;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public class ReturnAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// What the caller wants to do with the file, as the file module names it.
    /// </summary>
    public string Permission { get; set; }
}

public class ReturnAuthorizationHandler : AuthorizationHandler<ReturnAuthorizationRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    // The handler is a singleton, as every authorization handler in the platform is, so it cannot
    // hold the return services: their graph reaches IUserNameResolver, which is scoped, and
    // capturing it fails DI validation at startup rather than at the first request. A scope per
    // check is what the platform's own Func<UserManager<..>> factories amount to.
    public ReturnAuthorizationHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ReturnAuthorizationRequirement requirement)
    {
        if (await IsAllowedAsync(context, requirement))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    protected virtual async Task<bool> IsAllowedAsync(AuthorizationHandlerContext context, ReturnAuthorizationRequirement requirement)
    {
        if (context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator))
        {
            return true;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (context.Resource is not File file)
        {
            return false;
        }

        // The only way into this branch is a file just uploaded and not yet attached: a file dropped
        // from a draft is deleted rather than unowned, so it never returns to this pool. The
        // platform grants an unowned file to anyone at all; requiring authentication is as narrow
        // as this can get without an uploader on the model to compare against.
        if (file.OwnerIsEmpty())
        {
            return true;
        }

        if (!file.OwnerEntityType.EqualsIgnoreCase(nameof(Return)))
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();

        var returnService = scope.ServiceProvider.GetRequiredService<IReturnService>();
        var flowService = scope.ServiceProvider.GetRequiredService<IReturnFlowService>();

        var orderReturn = await returnService.GetNoCloneAsync(file.OwnerEntityId, ReturnResponseGroup.None.ToString());

        if (orderReturn == null)
        {
            return false;
        }

        if (await flowService.IsOwnedBy(orderReturn, GetUserId(context)))
        {
            return true;
        }

        // A colleague who may read the return may open its photos too, but not delete them.
        if (!requirement.Permission.EqualsIgnoreCase(FileExperienceApiModuleConstants.Security.Permissions.Read))
        {
            return false;
        }

        var organizationAccessService = scope.ServiceProvider.GetRequiredService<IReturnOrganizationAccessService>();
        var organizationId = context.User.GetCurrentOrganizationId();

        return organizationAccessService.IsVisibleToOrganization(orderReturn, organizationId) &&
            await organizationAccessService.CanViewAsync(context.User, organizationId);
    }

    protected virtual string GetUserId(AuthorizationHandlerContext context)
    {
        return context.User.GetUserId();
    }
}
