using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public class ReturnQueryHandler : IQueryHandler<ReturnQuery, Return>
{
    private readonly IReturnService _returnService;
    private readonly IReturnFlowService _flowService;

    public ReturnQueryHandler(IReturnService returnService, IReturnFlowService flowService)
    {
        _returnService = returnService;
        _flowService = flowService;
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

        // The same rule the organization list applies: a colleague's draft is not theirs to see yet.
        return !string.IsNullOrEmpty(request.OrganizationId) &&
            result.OrganizationId.EqualsIgnoreCase(request.OrganizationId) &&
            !result.Status.EqualsIgnoreCase(ReturnStatus.Draft)
            ? result
            : null;
    }
}
