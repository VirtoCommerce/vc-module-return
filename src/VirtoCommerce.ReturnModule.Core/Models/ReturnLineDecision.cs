namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnLineDecision
{
    // The return line's own id, not the order line's.
    public string LineItemId { get; set; }

    public int ApprovedQuantity { get; set; }

    public string RejectReason { get; set; }
}
