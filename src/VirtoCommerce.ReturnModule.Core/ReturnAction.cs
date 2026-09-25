namespace VirtoCommerce.ReturnModule.Core;

public static class ReturnAction
{
    public const string Edit = "edit";
    public const string Submit = "submit";
    public const string Cancel = "cancel";

    // The agent's side: which status it leads to depends on the quantities approved.
    public const string Authorize = "authorize";
}
