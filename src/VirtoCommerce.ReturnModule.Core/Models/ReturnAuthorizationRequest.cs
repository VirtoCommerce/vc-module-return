using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnAuthorizationRequest
{
    public string ReturnId { get; set; }

    public string RejectReason { get; set; }

    public IList<ReturnLineDecision> Items { get; set; } = [];
}
