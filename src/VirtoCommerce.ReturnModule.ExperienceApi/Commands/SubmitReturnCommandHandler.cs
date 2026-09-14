using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class SubmitReturnCommandHandler : IRequestHandler<SubmitReturnCommand, Return>
{
    private readonly IReturnFlowService _flowService;

    public SubmitReturnCommandHandler(IReturnFlowService flowService)
    {
        _flowService = flowService;
    }

    public virtual async Task<Return> Handle(SubmitReturnCommand request, CancellationToken cancellationToken)
    {
        var context = AbstractTypeFactory<ReturnFlowContext>.TryCreateInstance();
        context.CustomerId = request.CustomerId;

        try
        {
            return await _flowService.SubmitAsync(request.ReturnId, context, cancellationToken);
        }
        catch (ReturnFlowException exception)
        {
            // RETURN_QUANTITY_UNAVAILABLE lands here: the storefront shows it against the line and
            // lets the buyer adjust, rather than losing the draft.
            throw new ExecutionError(exception.Message, exception) { Code = exception.Code };
        }
    }
}
