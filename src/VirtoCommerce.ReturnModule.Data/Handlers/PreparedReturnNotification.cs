using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

/// <summary>
/// Everything a channel needs to tell the buyer about one return, read once and checked once.
/// </summary>
public class PreparedReturnNotification
{
    public Return Return { get; set; }

    public Store Store { get; set; }

    public ReturnBuyer Buyer { get; set; }

    /// <summary>
    /// Filled with the return, the buyer and the language, ready to render or to send.
    /// </summary>
    public ReturnEmailNotificationBase Notification { get; set; }

    /// <summary>
    /// The template the notification will render with, known to exist.
    /// </summary>
    public EmailNotificationTemplate Template { get; set; }
}
