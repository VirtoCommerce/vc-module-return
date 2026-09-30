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

// Registered only when the optional VirtoCommerce.PushMessages is installed: without it, a type that names
// IPushMessageService must never be constructed, so everything push-related lives here.
// The text is the matching email's subject, so an operator edits and translates the sentence once; a
// push-only notification kind would need a template entity in the Notifications module's schema.
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
            if (prepared.Buyer?.Member == null)
            {
                Logger.LogWarning(
                    "Customer {CustomerId} has no contact, no push message was created for return {ReturnNumber}.",
                    prepared.Return.CustomerId, prepared.Return.Number);

                continue;
            }

            // A subject edited in the admin may end with a newline, which a push message would keep.
            var shortMessage = (await RenderShortMessageAsync(prepared))?.Trim();

            if (string.IsNullOrEmpty(shortMessage))
            {
                Logger.LogWarning(
                    "Notification {NotificationType} rendered an empty subject, no push message was created for return {ReturnNumber}.",
                    prepared.Notification.Type, prepared.Return.Number);

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
