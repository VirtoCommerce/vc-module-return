using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.ExperienceApi.Extensions;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class CancelReturnCommandHandler : IRequestHandler<CancelReturnCommand, Return>
{
    private readonly IReturnFlowService _flowService;

    public CancelReturnCommandHandler(IReturnFlowService flowService)
    {
        _flowService = flowService;
    }

    public virtual async Task<Return> Handle(CancelReturnCommand request, CancellationToken cancellationToken)
    {
        var context = AbstractTypeFactory<ReturnFlowContext>.TryCreateInstance();
        context.CustomerId = request.CustomerId;

        try
        {
            return await _flowService.Cancel(request.ReturnId, request.Reason, context, cancellationToken);
        }
        catch (ReturnFlowException exception)
        {
            throw exception.ToExecutionError();
        }
    }
}
