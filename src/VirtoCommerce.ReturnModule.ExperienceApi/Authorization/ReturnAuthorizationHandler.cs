using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Authorization;

public class ReturnAuthorizationRequirement : IAuthorizationRequirement
{
}

public class ReturnAuthorizationHandler : AuthorizationHandler<ReturnAuthorizationRequirement>
{
    private readonly IReturnService _returnService;
    private readonly IReturnFlowService _flowService;

    public ReturnAuthorizationHandler(IReturnService returnService, IReturnFlowService flowService)
    {
        _returnService = returnService;
        _flowService = flowService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ReturnAuthorizationRequirement requirement)
    {
        if (await IsAllowedAsync(context))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    protected virtual async Task<bool> IsAllowedAsync(AuthorizationHandlerContext context)
    {
        if (context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator))
        {
            return true;
        }

        if (context.Resource is not File file)
        {
            return false;
        }

        // An unclaimed file is what the buyer has just uploaded and not yet attached to anything.
        if (file.OwnerIsEmpty())
        {
            return true;
        }

        if (!file.OwnerEntityType.EqualsIgnoreCase(nameof(Return)))
        {
            return false;
        }

        var orderReturn = await _returnService.GetNoCloneAsync(file.OwnerEntityId, ReturnResponseGroup.None.ToString());

        return orderReturn != null && await _flowService.IsOwnedByAsync(orderReturn, GetUserId(context));
    }

    protected virtual string GetUserId(AuthorizationHandlerContext context)
    {
        return context.User.GetUserId();
    }
}
