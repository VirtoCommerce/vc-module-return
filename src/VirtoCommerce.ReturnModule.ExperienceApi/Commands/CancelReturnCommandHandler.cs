using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;

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
            return await _flowService.CancelAsync(request.ReturnId, request.Reason, context, cancellationToken);
        }
        catch (ReturnFlowException exception)
        {
            // WRONG_STATUS lands here once an agent has moved the return on; the storefront turns it
            // into "this return can no longer be cancelled" rather than a generic failure.
            throw new ExecutionError(exception.Message, exception) { Code = exception.Code };
        }
    }
}
