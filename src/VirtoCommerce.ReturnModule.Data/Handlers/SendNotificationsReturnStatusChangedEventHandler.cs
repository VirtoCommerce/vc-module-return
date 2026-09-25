using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

public class SendNotificationsReturnStatusChangedEventHandler : ReturnStatusNotificationHandlerBase
{
    private readonly INotificationSender _notificationSender;
    private readonly ICustomerOrderService _orderService;

    public SendNotificationsReturnStatusChangedEventHandler(
        INotificationSearchService notificationSearchService,
        INotificationSender notificationSender,
        ICustomerOrderService orderService,
        IReturnService returnService,
        IReturnSettingsService settingsService,
        IStoreService storeService,
        IReturnBuyerResolver buyerResolver,
        ILogger<SendNotificationsReturnStatusChangedEventHandler> logger)
        : base(notificationSearchService, returnService, settingsService, storeService, buyerResolver, logger)
    {
        _notificationSender = notificationSender;
        _orderService = orderService;
    }

    protected override bool IsEnabled(ReturnStoreRules rules)
    {
        return rules.SendNotifications;
    }

    protected override void EnqueueSending(ReturnNotificationJobArgument argument)
    {
        BackgroundJob.Enqueue<SendNotificationsReturnStatusChangedEventHandler>(x => x.SendNotificationsAsync(new[] { argument }));
    }

    public virtual async Task SendNotificationsAsync(ReturnNotificationJobArgument[] jobArguments)
    {
        foreach (var prepared in await PrepareAsync(jobArguments))
        {
            var email = await GetRecipientEmailAsync(prepared);

            if (string.IsNullOrEmpty(email))
            {
                Logger.LogWarning(
                    "No email address for customer {CustomerId}, return {ReturnNumber} was not announced to the buyer.",
                    prepared.Return.CustomerId, prepared.Return.Number);

                continue;
            }

            var notification = prepared.Notification;

            notification.From = prepared.Store?.EmailWithName;
            notification.To = email;
            notification.TenantIdentity = new TenantIdentity(prepared.Return.Id, nameof(Return));

            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }
    }

    /// <summary>
    /// The address the order's own emails went to, as the Orders module picks it: the one the buyer
    /// entered on the order first, then the buyer's contact or login. A return is about that order,
    /// so it should not land in a different mailbox.
    /// </summary>
    protected virtual async Task<string> GetRecipientEmailAsync(PreparedReturnNotification prepared)
    {
        var order = string.IsNullOrEmpty(prepared.Return.OrderId)
            ? null
            : await _orderService.GetNoCloneAsync(prepared.Return.OrderId, CustomerOrderResponseGroup.WithAddresses.ToString());

        var orderEmail = order?.Addresses?.Select(x => x.Email).FirstOrDefault(x => !string.IsNullOrEmpty(x));

        return orderEmail ?? prepared.Buyer?.Email;
    }
}
