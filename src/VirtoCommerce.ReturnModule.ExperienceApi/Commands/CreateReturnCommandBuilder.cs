using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XOrder.Data.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class CreateReturnCommandBuilder : CommandBuilder<CreateReturnCommand, Return, CreateReturnCommandType, ReturnType>
{
    protected override string Name => "createReturn";

    public CreateReturnCommandBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    /// <summary>
    /// Two checks, both needed: the caller must be signed in, and must have access to the order the
    /// return is raised against. The customer is then taken from the token, so a draft can never be
    /// created on somebody else's behalf.
    /// </summary>
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, CreateReturnCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetCurrentUserId();

        if (string.IsNullOrEmpty(request.CustomerId))
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        // Builders are singletons — resolve per request, never through the constructor.
        var orderService = context.RequestServices.GetRequiredService<ICustomerOrderService>();
        var order = await orderService.GetNoCloneAsync(request.OrderId);

        await Authorize(context, order, new CanAccessOrderAuthorizationRequirement());
    }
}
