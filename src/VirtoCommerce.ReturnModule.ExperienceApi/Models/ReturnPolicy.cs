namespace VirtoCommerce.ReturnModule.ExperienceApi.Models;

/// <summary>
/// Per-store return rules the storefront needs before it can offer a return.
/// </summary>
public class ReturnPolicy
{
    public bool IsEnabled { get; set; }

    /// <summary>
    /// How long after delivery a line stays returnable.
    /// </summary>
    public int WindowDays { get; set; }
}
