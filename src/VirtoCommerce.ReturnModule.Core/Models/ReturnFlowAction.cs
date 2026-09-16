namespace VirtoCommerce.ReturnModule.Core.Models;

/// <summary>
/// One action the caller could attempt on a return, and whether it would be accepted right now.
/// </summary>
/// <remarks>
/// Unavailable actions are reported rather than omitted, so the storefront can show a disabled
/// control that says why instead of a control that silently vanishes.
/// </remarks>
public class ReturnFlowAction
{
    public string Name { get; set; }

    public bool IsAvailable { get; set; }

    /// <summary>
    /// Why the action would be refused, as the same code the mutation would fail with; null when
    /// the action is available.
    /// </summary>
    public string UnavailableReason { get; set; }
}
