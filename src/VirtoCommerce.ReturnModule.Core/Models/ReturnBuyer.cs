using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnBuyer
{
    // Null when only a login was found.
    public Member Member { get; set; }

    public string Email { get; set; }
}
