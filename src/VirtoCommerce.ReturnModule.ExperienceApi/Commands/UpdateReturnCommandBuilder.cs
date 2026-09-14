using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class UpdateReturnCommandBuilder : CommandBuilder<UpdateReturnCommand, Return, UpdateReturnCommandType, ReturnType>
{
    protected override string Name => "updateReturn";

    public UpdateReturnCommandBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    /// <summary>
    /// Ownership is settled by the flow service against the stored return, so all that is needed
    /// here is a signed-in caller and the customer taken from the token rather than from input.
    /// </summary>
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, UpdateReturnCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetCurrentUserId();

        if (string.IsNullOrEmpty(request.CustomerId))
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }
    }
}
