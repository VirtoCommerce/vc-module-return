using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Events
{
    /// <summary>
    /// Raised once per return whose status actually moved, whichever path wrote it: the storefront
    /// flow service, the admin REST endpoint, an import. Notification handlers subscribe here rather
    /// than to <see cref="ReturnChangedEvent"/>, so that a return saved for any other reason - a
    /// comment, an attachment, an autosaved draft - costs nothing.
    /// </summary>
    public class ReturnStatusChangedEvent : DomainEvent
    {
        public ReturnStatusChangedEvent(Return orderReturn, string fromStatus, string toStatus)
        {
            Return = orderReturn;
            FromStatus = fromStatus;
            ToStatus = toStatus;
        }

        public Return Return { get; }

        /// <summary>
        /// Null when the return was created already in <see cref="ToStatus"/> rather than moved into it.
        /// </summary>
        public string FromStatus { get; }

        public string ToStatus { get; }
    }
}
