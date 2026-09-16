using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
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

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, UpdateReturnCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetOwnCustomerId();
    }
}
