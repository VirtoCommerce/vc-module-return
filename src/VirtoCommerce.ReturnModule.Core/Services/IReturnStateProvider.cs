using System.Collections.Generic;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services;

/// <summary>
/// The transition table, declared in code. Every rule about which action a return's current status
/// allows lives here and nowhere else — the flow service asks it before acting, and the xAPI asks
/// it to tell the storefront which buttons to offer.
/// </summary>
/// <remarks>
/// A project that needs different rules — say, returns that may only be withdrawn while still a
/// draft — replaces this service rather than editing the flow service.
/// </remarks>
public interface IReturnStateProvider
{
    /// <summary>
    /// Every action this provider knows about, available or not, in the order they should be offered.
    /// </summary>
    IList<string> Actions { get; }

    bool IsAllowed(string action, string status);

    /// <summary>
    /// The status the action moves the return to, or null when it leaves the status alone.
    /// </summary>
    string GetNextStatus(string action, string status);

    /// <summary>
    /// Describes every known action against the return's current status.
    /// </summary>
    IList<ReturnFlowAction> GetActions(Return orderReturn);
}
