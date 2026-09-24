using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

public class SendNotificationsReturnStatusChangedEventHandler : ReturnStatusNotificationHandlerBase
{
    private readonly INotificationSender _notificationSender;
    private readonly ICustomerOrderService _orderService;
    private readonly IReturnSettingsService _settingsService;
    private readonly IMemberService _memberService;

    public SendNotificationsReturnStatusChangedEventHandler(
        INotificationSearchService notificationSearchService,
        INotificationSender notificationSender,
        ICustomerOrderService orderService,
        IReturnService returnService,
        IReturnSettingsService settingsService,
        IStoreService storeService,
        IReturnBuyerResolver buyerResolver,
        IMemberService memberService,
        ILogger<SendNotificationsReturnStatusChangedEventHandler> logger)
        : base(notificationSearchService, returnService, settingsService, storeService, buyerResolver, logger)
    {
        _notificationSender = notificationSender;
        _orderService = orderService;
        _settingsService = settingsService;
        _memberService = memberService;
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
            }
            else
            {
                await ScheduleAsync(prepared, prepared.Notification, email);
            }

            await SendOrganizationCopyAsync(prepared, email);
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

    protected virtual async Task SendOrganizationCopyAsync(PreparedReturnNotification prepared, string buyerEmail)
    {
        var orderReturn = prepared.Return;

        if (string.IsNullOrEmpty(orderReturn.OrganizationId))
        {
            return;
        }

        var rules = await _settingsService.GetRulesAsync(orderReturn.StoreId);

        if (!rules.NotifyOrganizationEmail)
        {
            return;
        }

        var organizationEmail = await GetOrganizationEmailAsync(orderReturn.OrganizationId);

        if (string.IsNullOrEmpty(organizationEmail))
        {
            Logger.LogWarning(
                "No email address for organization {OrganizationId}, return {ReturnNumber} was not copied to it.",
                orderReturn.OrganizationId, orderReturn.Number);

            return;
        }

        // A buyer whose own address is the organization's would get the same email twice.
        if (organizationEmail.EqualsIgnoreCase(buyerEmail))
        {
            return;
        }

        await ScheduleAsync(prepared, prepared.Notification.CloneTyped(), organizationEmail);
    }

    protected virtual async Task<string> GetOrganizationEmailAsync(string organizationId)
    {
        var organization = await _memberService.GetByIdAsync(organizationId, MemberResponseGroup.WithEmails.ToString());

        return organization?.Emails?.FirstOrDefault(x => !string.IsNullOrEmpty(x));
    }

    protected virtual Task ScheduleAsync(PreparedReturnNotification prepared, ReturnEmailNotificationBase notification, string email)
    {
        notification.From = prepared.Store?.EmailWithName;
        notification.To = email;
        notification.TenantIdentity = new TenantIdentity(prepared.Return.Id, nameof(Return));

        return _notificationSender.ScheduleSendNotificationAsync(notification);
    }
}
