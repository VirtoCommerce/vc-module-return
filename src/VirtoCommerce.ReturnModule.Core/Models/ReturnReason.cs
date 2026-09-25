namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnReason
{
    public string Code { get; set; }

    public string LocalizedName { get; set; }

    public bool RequiresComment { get; set; }
}
