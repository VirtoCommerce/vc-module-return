using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IReturnOrganizationAccessService _organizationAccessService;

    protected override string Name => "return";

    public ReturnQueryBuilder(IAuthorizationService authorizationService, IReturnOrganizationAccessService organizationAccessService)
        : base(authorizationService)
    {
        _organizationAccessService = organizationAccessService;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ReturnQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        request.CustomerId = context.GetOwnCustomerId();

        // The same organization the list shows, so that every row it offers also opens.
        var organizationId = context.GetCurrentOrganizationId();

        if (await _organizationAccessService.CanViewAsync(context.GetCurrentPrincipal(), organizationId))
        {
            request.OrganizationId = organizationId;
        }
    }
}
