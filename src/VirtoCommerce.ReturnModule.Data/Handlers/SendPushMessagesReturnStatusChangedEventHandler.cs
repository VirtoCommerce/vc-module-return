using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.PushMessages.Core.Models;
using VirtoCommerce.PushMessages.Core.Services;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

/// <summary>
/// The in-app half of telling the buyer where their return stands.
/// <para>
/// This class is registered only when VirtoCommerce.PushMessages is installed - the dependency is
/// optional, so its assembly may be absent, and a type that mentions <see cref="IPushMessageService"/>
/// must then never be constructed. Everything push-related lives here for that reason: the email
/// handler stays loadable whatever is installed.
/// </para>
/// <para>
/// The text comes from the matching email notification's subject template rather than from a
/// notification type of its own. A push short message and that subject say the same sentence -
/// "Return RET-0001 approved" - and this way an operator translates and edits it once, in the
/// admin, alongside every other notification. A push-only notification kind would need its own
/// template entity inside the Notifications module's schema, which is not ours to extend.
/// </para>
/// </summary>
public class SendPushMessagesReturnStatusChangedEventHandler : ReturnStatusNotificationHandlerBase
{
    private readonly INotificationTemplateRenderer _templateRenderer;
    private readonly IPushMessageService _pushMessageService;

    public SendPushMessagesReturnStatusChangedEventHandler(
        INotificationSearchService notificationSearchService,
        INotificationTemplateRenderer templateRenderer,
        IPushMessageService pushMessageService,
        IReturnService returnService,
        IReturnSettingsService settingsService,
        IStoreService storeService,
        IReturnBuyerResolver buyerResolver,
        ILogger<SendPushMessagesReturnStatusChangedEventHandler> logger)
        : base(notificationSearchService, returnService, settingsService, storeService, buyerResolver, logger)
    {
        _templateRenderer = templateRenderer;
        _pushMessageService = pushMessageService;
    }

    protected override bool IsEnabled(ReturnStoreRules rules)
    {
        return rules.SendPushNotifications;
    }

    protected override void EnqueueSending(ReturnNotificationJobArgument argument)
    {
        BackgroundJob.Enqueue<SendPushMessagesReturnStatusChangedEventHandler>(x => x.SendPushMessagesAsync(new[] { argument }));
    }

    public virtual async Task SendPushMessagesAsync(ReturnNotificationJobArgument[] jobArguments)
    {
        var pushMessages = new List<PushMessage>();

        foreach (var prepared in await PrepareAsync(jobArguments))
        {
            // PushMessages addresses members, and Return.CustomerId is usually a user id: a login
            // with no contact behind it has no one to receive the message.
            if (prepared.Buyer.Member == null)
            {
                Logger.LogWarning(
                    "Customer {CustomerId} has no contact, no push message was created for return {ReturnNumber}.",
                    prepared.Return.CustomerId, prepared.Return.Number);

                continue;
            }

            var shortMessage = await RenderShortMessageAsync(prepared);

            if (string.IsNullOrEmpty(shortMessage))
            {
                continue;
            }

            var pushMessage = AbstractTypeFactory<PushMessage>.TryCreateInstance();

            pushMessage.Topic = prepared.Return.Number;
            pushMessage.ShortMessage = shortMessage;
            pushMessage.StartDate = DateTime.UtcNow;
            pushMessage.Status = PushMessageStatus.Sent;
            pushMessage.MemberIds = [prepared.Buyer.Member.Id];

            pushMessages.Add(pushMessage);
        }

        if (pushMessages.Count > 0)
        {
            await _pushMessageService.SaveChangesAsync(pushMessages);
        }
    }

    protected virtual Task<string> RenderShortMessageAsync(PreparedReturnNotification prepared)
    {
        return _templateRenderer.RenderAsync(new NotificationRenderContext
        {
            Template = prepared.Template.Subject,
            Model = prepared.Notification,
            Language = prepared.Template.LanguageCode,
        });
    }
}
