using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class CreateReturnRequest
{
    public string OrderId { get; set; }

    /// <summary>
    /// Buyer's own purchase order reference; falls back to the order's when omitted.
    /// </summary>
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

    /// <summary>
    /// Files already uploaded into the return attachments scope, by URL. The line ends up owning
    /// exactly these — anything previously attached and missing here is released.
    /// </summary>
    public IList<string> AttachmentUrls { get; set; }
}
