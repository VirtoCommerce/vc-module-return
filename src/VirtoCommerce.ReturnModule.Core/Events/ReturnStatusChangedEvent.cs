using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Events
{
    // Raised once per return whose status moved, whichever path wrote it, so that a return saved for
    // any other reason - a comment, an attachment, an autosaved draft - costs its subscribers nothing.
    public class ReturnStatusChangedEvent : DomainEvent
    {
        public ReturnStatusChangedEvent(Return orderReturn, string fromStatus, string toStatus)
        {
            Return = orderReturn;
            FromStatus = fromStatus;
            ToStatus = toStatus;
        }

        public Return Return { get; }

        // Null when the return was created already in ToStatus rather than moved into it.
        public string FromStatus { get; }

        public string ToStatus { get; }
    }
}
