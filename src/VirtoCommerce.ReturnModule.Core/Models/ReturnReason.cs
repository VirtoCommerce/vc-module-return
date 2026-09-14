namespace VirtoCommerce.ReturnModule.Core.Models;

/// <summary>
/// One option in the store's return reason dictionary.
/// </summary>
public class ReturnReason
{
    public string Code { get; set; }

    /// <summary>
    /// Name in the requested language, falling back to the code when a store added a value but no
    /// translation for it.
    /// </summary>
    public string LocalizedName { get; set; }

    public bool RequiresComment { get; set; }
}
