using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnFlowService
{
    Task<Return> CreateDraft(CreateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default);

    Task<Return> UpdateDraft(UpdateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default);

    Task<Return> Submit(string returnId, ReturnFlowContext context, CancellationToken cancellationToken = default);

    Task<Return> Cancel(string returnId, string reason, ReturnFlowContext context, CancellationToken cancellationToken = default);

    IList<ReturnFlowAction> GetAvailableActions(Return orderReturn);

    Task<bool> IsOwnedBy(Return orderReturn, string customerId);
}
