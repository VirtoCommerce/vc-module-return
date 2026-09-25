using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnQueryBuilder : QueryBuilder<ReturnQuery, Return, ReturnType>
{
    protected override string Name => "return";

    public ReturnQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetOwnCustomerId();

        // The same organization the list shows, so that every row it offers also opens.
        var organizationId = context.GetCurrentOrganizationId();
        var organizationAccessService = context.RequestServices.GetRequiredService<IReturnOrganizationAccessService>();

        if (await organizationAccessService.CanViewAsync(context.GetCurrentPrincipal(), organizationId))
        {
            request.OrganizationId = organizationId;
        }
    }
}
