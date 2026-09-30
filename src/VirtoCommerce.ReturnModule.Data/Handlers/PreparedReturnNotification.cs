using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

public class PreparedReturnNotification
{
    public Return Return { get; set; }

    public Store Store { get; set; }

    // Null when neither a contact nor a login was found for the return's customer.
    public ReturnBuyer Buyer { get; set; }

    public ReturnEmailNotificationBase Notification { get; set; }

    public EmailNotificationTemplate Template { get; set; }
}
