using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.ReturnModule.Data.BackgroundJobs;
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

    // The static facade, not an injected IBackgroundJob: RegisterEventHandler resolves this handler once from
    // the root provider and holds it for the process lifetime, so it must not capture a Scoped dependency.
    protected override Task EnqueueSending(ReturnNotificationJobArgument argument)
    {
        return BackgroundJob.Enqueue<SendReturnNotificationsJobHandler>(argument);
    }

    // Also the target of jobs Hangfire queued before the move to the Platform.Core job API: keep the signature.

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

    // Where the order's own emails went, as the Orders module picks it: the address on the order first,
    // then the buyer's contact or login. A return is about that order, so it lands in the same mailbox.
    protected virtual async Task<string> GetRecipientEmailAsync(PreparedReturnNotification prepared)
    {
        var order = string.IsNullOrEmpty(prepared.Return.OrderId)
            ? null
            : await _orderService.GetNoCloneAsync(prepared.Return.OrderId, CustomerOrderResponseGroup.WithAddresses.ToString());

        var orderEmail = order?.Addresses?.Select(x => x.Email).FirstOrDefault(x => !string.IsNullOrEmpty(x));

        return orderEmail ?? prepared.Buyer?.Email;
    }
}
