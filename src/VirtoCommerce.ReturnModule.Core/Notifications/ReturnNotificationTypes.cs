using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VirtoCommerce.ReturnModule.Core.Notifications
{
    /// <summary>
    /// Which status the buyer is told about, and by which notification. Shared by the email and the
    /// push handler so that the two can never disagree about what deserves telling the buyer.
    /// </summary>
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

                // The spelling the module shipped with. Both are in the Return.Status dictionary, so
                // an operator can pick either, and a buyer whose return was cancelled should hear
                // about it whichever one was picked.
                [LegacyCancelledSpelling] = nameof(ReturnCancelledEmailNotification),
            });

        public const string LegacyCancelledSpelling = "Canceled";
    }
}
