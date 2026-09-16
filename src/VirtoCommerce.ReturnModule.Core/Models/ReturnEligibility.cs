namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnEligibility
{
    public bool IsEligible { get; set; }

    public string Reason { get; set; }

    public static ReturnEligibility Eligible() => new() { IsEligible = true };

    public static ReturnEligibility Ineligible(string reason) => new() { Reason = reason };
}
