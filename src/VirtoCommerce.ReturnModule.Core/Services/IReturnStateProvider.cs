using System.Collections.Generic;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

public interface IReturnStateProvider
{
    bool IsAllowed(string action, string status);

    string GetNextStatus(string action, string status);

    IList<ReturnFlowAction> GetActions(Return orderReturn);

    /// <summary>
    /// Whether a status may be set by hand - an edit rather than one of the flow's actions.
    /// <paramref name="orderReturn"/> is the return as stored, or null for one being created.
    /// </summary>
    bool CanSetStatus(Return orderReturn, string newStatus);
}
