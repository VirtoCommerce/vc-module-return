using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class UpdateReturnRequest
{
    public string ReturnId { get; set; }

    public string CustomerReference { get; set; }

    public string CustomerComment { get; set; }

    /// <summary>
    /// The draft's lines as they should end up. Lines missing from this list are dropped, which is
    /// how the buyer removes one; null leaves the existing lines alone.
    /// </summary>
    public IList<CreateReturnItemRequest> Items { get; set; }
}
