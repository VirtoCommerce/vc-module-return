namespace VirtoCommerce.ReturnModule.Core.Models;

/// <summary>
/// One row of the transition table: an action, the status it may be taken from, and where it
/// leaves the return.
/// </summary>
public class ReturnStateTransition
{
    public string Action { get; set; }

    public string FromStatus { get; set; }

    /// <summary>
    /// Null when the action does not move the return — editing a draft leaves it a draft.
    /// </summary>
    public string ToStatus { get; set; }
}
