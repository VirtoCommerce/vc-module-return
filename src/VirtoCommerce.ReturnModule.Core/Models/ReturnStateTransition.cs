namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnStateTransition
{
    public string Action { get; set; }

    public string FromStatus { get; set; }

    public string ToStatus { get; set; }
}
