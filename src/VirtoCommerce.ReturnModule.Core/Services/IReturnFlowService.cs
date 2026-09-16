using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnFlowService
{
    Task<Return> CreateDraftAsync(CreateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default);

    Task<Return> UpdateDraftAsync(UpdateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default);

    Task<Return> SubmitAsync(string returnId, ReturnFlowContext context, CancellationToken cancellationToken = default);

    Task<Return> CancelAsync(string returnId, string reason, ReturnFlowContext context, CancellationToken cancellationToken = default);

    IList<ReturnFlowAction> GetAvailableActions(Return orderReturn);
}
