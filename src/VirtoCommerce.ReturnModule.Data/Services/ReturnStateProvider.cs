using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
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
        new() { Action = ReturnAction.Authorize, FromStatus = ReturnStatus.Requested },
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
