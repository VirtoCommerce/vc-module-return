using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class CreateReturnRequest
{
    public string OrderId { get; set; }

    public string CustomerReference { get; set; }

    public string CustomerComment { get; set; }

    public IList<CreateReturnItemRequest> Items { get; set; }
}
