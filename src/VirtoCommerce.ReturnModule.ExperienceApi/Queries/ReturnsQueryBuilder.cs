using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;
using VirtoCommerce.ReturnModule.ExperienceApi.Models;
using VirtoCommerce.ReturnModule.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnsQueryBuilder : SearchQueryBuilder<ReturnsQuery, ReturnSearchResult, Return, ReturnType>
{
    protected override string Name => "returns";

    public ReturnsQueryBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnsQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetOwnCustomerId();

        if (request.Scope != ReturnScope.Organization)
        {
            return;
        }

        // The organization the contact has switched to, as the organization orders list uses: a
        // contact of several organizations sees one at a time.
        var organizationId = context.GetCurrentOrganizationId();
        var organizationAccessService = context.RequestServices.GetRequiredService<IReturnOrganizationAccessService>();

        if (!await organizationAccessService.CanViewAsync(context.GetCurrentPrincipal(), organizationId))
        {
            throw AuthorizationError.Forbidden("The organization's returns are not available to this contact.");
        }

        request.OrganizationId = organizationId;
    }
}
