using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class OrganizationReturnsQueryBuilder : ReturnSearchQueryBuilderBase<OrganizationReturnsQuery, ReturnSearchResult, Return, ReturnType>
{
    protected override string Name => "organizationReturns";

    public OrganizationReturnsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, OrganizationReturnsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        var accessService = context.RequestServices.GetRequiredService<IReturnAccessService>();

        if (!await accessService.CanViewOrganizationAsync(context.GetOwnCustomerId(), request.OrganizationId))
        {
            throw AuthorizationError.Forbidden("The organization's returns are not available to this contact.");
        }
    }
}
