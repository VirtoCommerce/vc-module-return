using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

/// <summary>
/// Owns the return's lifecycle. Everything that moves a return between states goes through here,
/// so the rules live in one place rather than in each caller.
/// </summary>
public interface IReturnFlowService
{
    /// <summary>
    /// Starts a return in <see cref="ReturnStatus.Draft"/>, snapshotting product and price details
    /// from the order line.
    /// </summary>
    /// <remarks>
    /// A draft deliberately holds no quantity — an abandoned one must not block a line forever, so
    /// availability is checked when the return is submitted, not here.
    /// </remarks>
    Task<Return> CreateDraftAsync(CreateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits a draft: references, comments, reasons and quantities. Only drafts are editable.
    /// </summary>
    Task<Return> UpdateDraftAsync(UpdateReturnRequest request, ReturnFlowContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hands the draft to the returns agent, moving it to <see cref="ReturnStatus.Requested"/>.
    /// </summary>
    /// <remarks>
    /// This is where quantity is finally checked and claimed: two buyers in one organization can be
    /// drafting against the same order, and the second one must not be allowed to over-return.
    /// </remarks>
    Task<Return> SubmitAsync(string returnId, ReturnFlowContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws the buyer's own return, moving it to <see cref="ReturnStatus.Cancelled"/>.
    /// </summary>
    /// <remarks>
    /// Whatever quantity the return was holding goes back to available on its own, because
    /// availability is derived from status rather than kept in a counter.
    /// </remarks>
    Task<Return> CancelAsync(string returnId, string reason, ReturnFlowContext context, CancellationToken cancellationToken = default);
}
