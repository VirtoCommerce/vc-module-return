namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnFlowAction
{
    public string Name { get; set; }

    public bool IsAvailable { get; set; }

    public string UnavailableReason { get; set; }
}
