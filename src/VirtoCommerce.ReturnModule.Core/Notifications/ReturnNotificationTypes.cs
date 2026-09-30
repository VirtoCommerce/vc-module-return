using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VirtoCommerce.ReturnModule.Core.Notifications
{
    // One map for the email and the push handler, so the two cannot disagree on what the buyer is told.
    public static class ReturnNotificationTypes
    {
        public static IReadOnlyDictionary<string, string> ByStatus { get; } =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ReturnStatus.Requested] = nameof(ReturnRegisteredEmailNotification),
                [ReturnStatus.Approved] = nameof(ReturnApprovedEmailNotification),
                [ReturnStatus.PartiallyApproved] = nameof(ReturnPartiallyApprovedEmailNotification),
                [ReturnStatus.Rejected] = nameof(ReturnRejectedEmailNotification),
                [ReturnStatus.Cancelled] = nameof(ReturnCancelledEmailNotification),
                [ReturnStatus.LegacyCancelled] = nameof(ReturnCancelledEmailNotification),
            });
    }
}
