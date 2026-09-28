using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnRequestValidationContext
{
    public string CustomerReference { get; set; }

    public string CustomerComment { get; set; }

    public IList<CreateReturnItemRequest> Items { get; set; } = [];

    public IList<string> Reasons { get; set; } = [];

    public IList<string> ReasonsRequiringComment { get; set; } = [];

    // A draft is saved on every keystroke, so a line without a reason is an unfinished one, not an
    // invalid one. Submit is where that stops being true.
    public bool RequireReason { get; set; }
}
