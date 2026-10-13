using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class CancelReturnCommandBuilder : ReturnCommandBuilderBase<CancelReturnCommand, Return, CancelReturnCommandType, ReturnType>
{
    protected override string Name => "cancelReturn";

    public CancelReturnCommandBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, CancelReturnCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetOwnCustomerId();
    }
}
