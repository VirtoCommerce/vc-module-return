namespace VirtoCommerce.ReturnModule.Core;

/// <summary>
/// Things that can be done to a return. Names are stable identifiers, not labels — the storefront
/// localizes them by code, the same way it localizes ineligibility reasons.
/// </summary>
public static class ReturnAction
{
    public const string Edit = "edit";
    public const string Submit = "submit";
    public const string Cancel = "cancel";
}
