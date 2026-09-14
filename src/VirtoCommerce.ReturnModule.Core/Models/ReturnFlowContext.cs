namespace VirtoCommerce.ReturnModule.Core.Models;

/// <summary>
/// Who is driving a return transition. Comes from the caller's identity, never from client input —
/// the flow service stamps ownership from it.
/// </summary>
public class ReturnFlowContext
{
    public string CustomerId { get; set; }

    public string CustomerName { get; set; }

    public string StoreId { get; set; }
}
