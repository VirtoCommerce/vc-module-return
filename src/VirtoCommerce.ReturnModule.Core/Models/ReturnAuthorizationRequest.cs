using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

/// <summary>
/// An agent's decision on a requested return: how much of every line is taken back.
/// </summary>
public class ReturnAuthorizationRequest
{
    public string ReturnId { get; set; }

    /// <summary>
    /// Why the return, or the part of it that was not approved, was declined.
    /// </summary>
    public string RejectReason { get; set; }

    /// <summary>
    /// One per line of the return: a line left out is a line nobody looked at.
    /// </summary>
    public IList<ReturnLineDecision> Items { get; set; } = [];
}
