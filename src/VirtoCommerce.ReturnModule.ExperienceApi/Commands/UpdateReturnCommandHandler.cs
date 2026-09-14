using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Commands;

public class UpdateReturnCommandHandler : IRequestHandler<UpdateReturnCommand, Return>
{
    private readonly IReturnFlowService _flowService;

    public UpdateReturnCommandHandler(IReturnFlowService flowService)
    {
        _flowService = flowService;
    }

    public virtual async Task<Return> Handle(UpdateReturnCommand request, CancellationToken cancellationToken)
    {
        var flowRequest = AbstractTypeFactory<UpdateReturnRequest>.TryCreateInstance();
        flowRequest.ReturnId = request.ReturnId;
        flowRequest.CustomerReference = request.CustomerReference;
        flowRequest.CustomerComment = request.CustomerComment;
        flowRequest.Items = request.Items;

        var context = AbstractTypeFactory<ReturnFlowContext>.TryCreateInstance();
        context.CustomerId = request.CustomerId;

        try
        {
            return await _flowService.UpdateDraftAsync(flowRequest, context, cancellationToken);
        }
        catch (ReturnFlowException exception)
        {
            throw new ExecutionError(exception.Message, exception) { Code = exception.Code };
        }
    }
}
