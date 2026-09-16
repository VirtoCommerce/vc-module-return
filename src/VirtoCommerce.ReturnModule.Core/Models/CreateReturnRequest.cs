using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class CreateReturnRequest
{
    public string OrderId { get; set; }

    public string CustomerReference { get; set; }

    public string CustomerComment { get; set; }

    public IList<CreateReturnItemRequest> Items { get; set; }
}

public class CreateReturnItemRequest
{
    public string OrderLineItemId { get; set; }

    public int Quantity { get; set; }

    public string ReasonCode { get; set; }

    public string ReasonComment { get; set; }

    public string SerialNumber { get; set; }

    public IList<string> AttachmentUrls { get; set; }
}
