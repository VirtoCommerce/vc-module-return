namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnLineDecision
{
    /// <summary>
    /// The return line's own id, not the order line's.
    /// </summary>
    public string LineItemId { get; set; }

    /// <summary>
    /// From 0, which declines the line, up to the requested quantity, which approves it in full.
    /// </summary>
    public int ApprovedQuantity { get; set; }

    public string RejectReason { get; set; }
}
