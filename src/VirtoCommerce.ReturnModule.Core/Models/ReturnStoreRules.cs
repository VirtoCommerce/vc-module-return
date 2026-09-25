using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.Core.Models;

// What a store demands of a return, read once instead of a setting at a time.
public class ReturnStoreRules
{
    public IList<string> Reasons { get; set; } = [];

    public IList<string> ReasonsRequiringComment { get; set; } = [];

    public bool AttachmentsRequired { get; set; }

    public bool SendNotifications { get; set; }

    public bool SendPushNotifications { get; set; }
}
