using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnQueryHandler : IQueryHandler<ReturnQuery, Return>
{
    private readonly IReturnService _returnService;
    private readonly IReturnFlowService _flowService;
    private readonly IReturnOrganizationAccessService _organizationAccessService;

    public ReturnQueryHandler(IReturnService returnService, IReturnFlowService flowService, IReturnOrganizationAccessService organizationAccessService)
    {
        _returnService = returnService;
        _flowService = flowService;
        _organizationAccessService = organizationAccessService;
    }

    public virtual async Task<Return> Handle(ReturnQuery request, CancellationToken cancellationToken)
    {
        var result = await _returnService.GetNoCloneAsync(request.Id, ReturnResponseGroup.None.ToString());

        if (result == null)
        {
            return null;
        }

        if (await _flowService.IsOwnedBy(result, request.CustomerId))
        {
            return result;
        }

        return _organizationAccessService.IsVisibleToOrganization(result, request.OrganizationId) ? result : null;
    }
}
