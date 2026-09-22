namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnFlowContext
{
    public string CustomerId { get; set; }

    public string CustomerName { get; set; }

    public string StoreId { get; set; }

    /// <summary>
    /// Culture of the storefront the buyer is on. Snapshotted onto the return so that later
    /// notifications speak the language the return was raised in.
    /// </summary>
    public string LanguageCode { get; set; }
}
