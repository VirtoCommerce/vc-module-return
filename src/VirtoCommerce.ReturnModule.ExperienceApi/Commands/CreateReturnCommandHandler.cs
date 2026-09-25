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

public class CreateReturnCommandHandler : IRequestHandler<CreateReturnCommand, Return>
{
    private readonly IReturnFlowService _flowService;

    public CreateReturnCommandHandler(IReturnFlowService flowService)
    {
        _flowService = flowService;
    }

    public virtual async Task<Return> Handle(CreateReturnCommand request, CancellationToken cancellationToken)
    {
        var flowRequest = AbstractTypeFactory<CreateReturnRequest>.TryCreateInstance();
        flowRequest.OrderId = request.OrderId;
        flowRequest.CustomerReference = request.CustomerReference;
        flowRequest.CustomerComment = request.CustomerComment;
        flowRequest.Items = request.Items;

        var context = AbstractTypeFactory<ReturnFlowContext>.TryCreateInstance();
        context.CustomerId = request.CustomerId;
        context.LanguageCode = request.LanguageCode;

        try
        {
            return await _flowService.CreateDraft(flowRequest, context, cancellationToken);
        }
        catch (ReturnFlowException exception)
        {
            throw exception.ToExecutionError();
        }
    }
}
