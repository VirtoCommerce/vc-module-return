using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

// On the CRUD event rather than in the flow service because every writer passes there: the storefront
// flow, PUT /api/return and any other IReturnService caller. What is worth announcing is the handlers' call.
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

                case EntryState.Modified when IsReportable(changedEntry.OldEntry?.Status, changedEntry.NewEntry?.Status):
                    await Publish(changedEntry.NewEntry, changedEntry.OldEntry?.Status);
                    break;
            }
        }
    }

    protected virtual Task Publish(Return orderReturn, string fromStatus)
    {
        return _eventPublisher.Publish(new ReturnStatusChangedEvent(orderReturn, fromStatus, orderReturn.Status));
    }

    protected virtual bool IsReportable(string status)
    {
        return !string.IsNullOrEmpty(status);
    }

    protected virtual bool IsReportable(string oldStatus, string newStatus)
    {
        return IsReportable(newStatus) && !IsSameStatus(oldStatus, newStatus);
    }

    // Moving between the two spellings of cancelled is not a change of status.
    protected virtual bool IsSameStatus(string oldStatus, string newStatus)
    {
        return Normalize(oldStatus).EqualsIgnoreCase(Normalize(newStatus));
    }

    protected virtual string Normalize(string status)
    {
        return ReturnStatus.Normalize(status);
    }
}
