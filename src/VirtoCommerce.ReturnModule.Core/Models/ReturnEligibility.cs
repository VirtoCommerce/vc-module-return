namespace VirtoCommerce.ReturnModule.Core.Models;

/// <summary>
/// Whether a return may be requested against an order at all, and why not.
/// </summary>
public class ReturnEligibility
{
    public bool IsEligible { get; set; }

    /// <summary>
    /// One of <see cref="ReturnIneligibilityReason"/>, or null when eligible.
    /// </summary>
    public string Reason { get; set; }

    public static ReturnEligibility Eligible() => new() { IsEligible = true };

    public static ReturnEligibility Ineligible(string reason) => new() { Reason = reason };
}
