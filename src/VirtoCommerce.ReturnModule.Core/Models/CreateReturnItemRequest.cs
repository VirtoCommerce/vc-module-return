using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class CreateReturnItemRequest
{
    public string OrderLineItemId { get; set; }

    public int Quantity { get; set; }

    public string ReasonCode { get; set; }

    public string ReasonComment { get; set; }

    public string SerialNumber { get; set; }

    public IList<string> AttachmentUrls { get; set; }
}
