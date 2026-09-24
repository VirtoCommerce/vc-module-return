using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnStateProvider : IReturnStateProvider
{
    protected virtual IList<ReturnStateTransition> Transitions { get; } =
    [
        new() { Action = ReturnAction.Edit, FromStatus = ReturnStatus.Draft },
        new() { Action = ReturnAction.Submit, FromStatus = ReturnStatus.Draft, ToStatus = ReturnStatus.Requested },
        new() { Action = ReturnAction.Cancel, FromStatus = ReturnStatus.Draft, ToStatus = ReturnStatus.Cancelled },
        new() { Action = ReturnAction.Cancel, FromStatus = ReturnStatus.Requested, ToStatus = ReturnStatus.Cancelled },

        // No target: Approved, PartiallyApproved or Rejected follows from the quantities approved.
        // New is what the admin creates a return as, so it is decided on the same way.
        new() { Action = ReturnAction.Authorize, FromStatus = ReturnStatus.Requested },
        new() { Action = ReturnAction.Authorize, FromStatus = ReturnStatus.New },
    ];

    /// <summary>
    /// Taken by an agent in the back office, so never offered to the buyer as one of their actions.
    /// </summary>
    protected virtual ISet<string> AgentActions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnAction.Authorize,
        };

    public virtual bool IsAllowed(string action, string status)
    {
        return Find(action, status) != null;
    }

    public virtual string GetNextStatus(string action, string status)
    {
        return Find(action, status)?.ToStatus;
    }

    public virtual IList<ReturnFlowAction> GetActions(Return orderReturn)
    {
        ArgumentNullException.ThrowIfNull(orderReturn);

        return GetActionNames()
            .Select(action =>
            {
                var result = AbstractTypeFactory<ReturnFlowAction>.TryCreateInstance();
                result.Name = action;
                result.IsAvailable = IsAllowed(action, orderReturn.Status);
                result.UnavailableReason = result.IsAvailable ? null : ReturnFlowError.WrongStatus;

                return result;
            })
            .ToList();
    }

    public virtual bool CanSetStatus(Return orderReturn, string newStatus)
    {
        var oldStatus = Normalize(orderReturn?.Status);

        if (oldStatus.EqualsIgnoreCase(Normalize(newStatus)))
        {
            return true;
        }

        if (FlowStatuses.Contains(newStatus ?? string.Empty) || ClosedStatuses.Contains(oldStatus))
        {
            return false;
        }

        // Once lines are decided, an edit can only carry the return out - not cancel it, reopen it
        // or decide it again.
        var isDecided = orderReturn?.LineItems?.Any(x => DecidedItemStates.Contains(x.ItemState ?? string.Empty)) == true;

        return !isDecided || FulfilmentStatuses.Contains(newStatus ?? string.Empty);
    }

    /// <summary>
    /// Set only by the flow's actions: submitting, and authorizing.
    /// </summary>
    protected virtual ISet<string> FlowStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.Draft,
            ReturnStatus.Requested,
            ReturnStatus.Approved,
            ReturnStatus.PartiallyApproved,
            ReturnStatus.Rejected,
        };

    /// <summary>
    /// Left only through the flow, or not at all: a draft and a request are the buyer's, a declined or
    /// cancelled return is closed.
    /// </summary>
    protected virtual ISet<string> ClosedStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.Draft,
            ReturnStatus.Requested,
            ReturnStatus.Rejected,
            ReturnStatus.Cancelled,
        };

    /// <summary>
    /// Where a decided return goes next.
    /// </summary>
    protected virtual ISet<string> FulfilmentStatuses { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.AwaitingDelivery,
            ReturnStatus.Received,
            ReturnStatus.Processing,
            ReturnStatus.Completed,
        };

    protected virtual ISet<string> DecidedItemStates { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnItemState.Approved,
            ReturnItemState.Rejected,
        };

    protected virtual string Normalize(string status)
    {
        return status.EqualsIgnoreCase(ReturnNotificationTypes.LegacyCancelledSpelling)
            ? ReturnStatus.Cancelled
            : status ?? string.Empty;
    }

    protected virtual IEnumerable<string> GetActionNames()
    {
        return Transitions
            .Select(x => x.Action)
            .Where(x => !AgentActions.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    protected virtual ReturnStateTransition Find(string action, string status)
    {
        return Transitions.FirstOrDefault(x =>
            x.Action.EqualsIgnoreCase(action) &&
            x.FromStatus.EqualsIgnoreCase(status ?? string.Empty));
    }
}
