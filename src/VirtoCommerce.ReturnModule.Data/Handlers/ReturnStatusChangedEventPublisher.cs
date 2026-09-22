using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

/// <summary>
/// Turns "a return was saved" into "a return changed status". Sitting on the CRUD event rather than
/// inside the flow service is deliberate: the agent side has no entry point yet, and until it lands
/// a status is moved from the admin blade - which never calls the flow service.
/// </summary>
public class ReturnStatusChangedEventPublisher : IEventHandler<ReturnChangedEvent>
{
    private readonly IEventPublisher _eventPublisher;

    public ReturnStatusChangedEventPublisher(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public virtual async Task Handle(ReturnChangedEvent message)
    {
        foreach (var changedEntry in message.ChangedEntries)
        {
            switch (changedEntry.EntryState)
            {
                // GenericChangedEntry copies the same instance into both slots for an addition, so
                // there is nothing to compare - the return arrived in whatever status it carries.
                case EntryState.Added when IsReportable(changedEntry.NewEntry?.Status):
                    await Publish(changedEntry.NewEntry, fromStatus: null);
                    break;

                case EntryState.Modified when !IsSameStatus(changedEntry.OldEntry?.Status, changedEntry.NewEntry?.Status):
                    await Publish(changedEntry.NewEntry, changedEntry.OldEntry?.Status);
                    break;
            }
        }
    }

    protected virtual Task Publish(Return orderReturn, string fromStatus)
    {
        return _eventPublisher.Publish(new ReturnStatusChangedEvent(orderReturn, fromStatus, orderReturn.Status));
    }

    /// <summary>
    /// A draft is not an event anyone is waiting for: the buyer is still filling it in, and the
    /// storefront creates one per wizard run.
    /// </summary>
    protected virtual bool IsReportable(string status)
    {
        return !string.IsNullOrEmpty(status) && !status.EqualsIgnoreCase(ReturnStatus.Draft);
    }

    /// <summary>
    /// "Canceled" is the spelling the module shipped with and "Cancelled" the one it uses now. Both
    /// are in the status dictionary, so an operator can pick either, and moving between them is not
    /// a change of status.
    /// </summary>
    protected virtual bool IsSameStatus(string oldStatus, string newStatus)
    {
        return Normalize(oldStatus).EqualsIgnoreCase(Normalize(newStatus));
    }

    protected virtual string Normalize(string status)
    {
        return status.EqualsIgnoreCase(LegacyCancelledSpelling)
            ? ReturnStatus.Cancelled
            : status ?? string.Empty;
    }

    protected const string LegacyCancelledSpelling = "Canceled";
}
