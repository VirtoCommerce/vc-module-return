using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnBuyer
{
    /// <summary>
    /// The contact behind the return; null when only a login was found.
    /// </summary>
    public Member Member { get; set; }

    public string Email { get; set; }
}
