using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.PushMessages.Core.Models;
using VirtoCommerce.PushMessages.Core.Services;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
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
public class SendPushMessagesReturnStatusChangedEventHandler : IEventHandler<ReturnStatusChangedEvent>
{
    private readonly INotificationSearchService _notificationSearchService;
    private readonly INotificationTemplateRenderer _templateRenderer;
    private readonly IPushMessageService _pushMessageService;
    private readonly IReturnService _returnService;
    private readonly IReturnSettingsService _settingsService;
    private readonly IStoreService _storeService;
    private readonly ILogger<SendPushMessagesReturnStatusChangedEventHandler> _logger;

    public SendPushMessagesReturnStatusChangedEventHandler(
        INotificationSearchService notificationSearchService,
        INotificationTemplateRenderer templateRenderer,
        IPushMessageService pushMessageService,
        IReturnService returnService,
        IReturnSettingsService settingsService,
        IStoreService storeService,
        ILogger<SendPushMessagesReturnStatusChangedEventHandler> logger)
    {
        _notificationSearchService = notificationSearchService;
        _templateRenderer = templateRenderer;
        _pushMessageService = pushMessageService;
        _returnService = returnService;
        _settingsService = settingsService;
        _storeService = storeService;
        _logger = logger;
    }

    public virtual async Task Handle(ReturnStatusChangedEvent message)
    {
        var orderReturn = message.Return;

        var notificationTypeName = GetNotificationTypeName(message.ToStatus);

        if (notificationTypeName == null)
        {
            return;
        }

        var rules = await _settingsService.GetRulesAsync(orderReturn.StoreId);

        if (!rules.SendPushNotifications)
        {
            return;
        }

        if (string.IsNullOrEmpty(orderReturn.CustomerId))
        {
            _logger.LogWarning(
                "Return {ReturnNumber} has no customer, no push message was created.",
                orderReturn.Number);

            return;
        }

        EnqueueSending(new ReturnNotificationJobArgument
        {
            ReturnId = orderReturn.Id,
            StoreId = orderReturn.StoreId,
            CustomerId = orderReturn.CustomerId,
            NotificationTypeName = notificationTypeName,
        });
    }

    protected virtual void EnqueueSending(ReturnNotificationJobArgument argument)
    {
        BackgroundJob.Enqueue<SendPushMessagesReturnStatusChangedEventHandler>(x => x.SendPushMessagesAsync(new[] { argument }));
    }

    public virtual async Task SendPushMessagesAsync(ReturnNotificationJobArgument[] jobArguments)
    {
        var returnsById = (await _returnService.GetAsync(
                jobArguments.Select(x => x.ReturnId).Distinct().ToList(),
                ReturnResponseGroup.None.ToString()))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var pushMessages = new List<PushMessage>();

        foreach (var jobArgument in jobArguments)
        {
            if (!returnsById.TryGetValue(jobArgument.ReturnId, out var orderReturn))
            {
                continue;
            }

            var shortMessage = await RenderShortMessageAsync(jobArgument, orderReturn);

            if (string.IsNullOrEmpty(shortMessage))
            {
                continue;
            }

            var pushMessage = AbstractTypeFactory<PushMessage>.TryCreateInstance();

            pushMessage.Topic = orderReturn.Number;
            pushMessage.ShortMessage = shortMessage;
            pushMessage.StartDate = DateTime.UtcNow;
            pushMessage.Status = PushMessageStatus.Sent;
            pushMessage.MemberIds = [jobArgument.CustomerId];

            pushMessages.Add(pushMessage);
        }

        if (pushMessages.Count > 0)
        {
            await _pushMessageService.SaveChangesAsync(pushMessages);
        }
    }

    protected virtual async Task<string> RenderShortMessageAsync(ReturnNotificationJobArgument jobArgument, Return orderReturn)
    {
        var notification = await _notificationSearchService.GetNotificationAsync(
            jobArgument.NotificationTypeName,
            new TenantIdentity(jobArgument.StoreId, nameof(Store)));

        if (notification is not ReturnEmailNotificationBase returnNotification)
        {
            _logger.LogWarning(
                "Notification {NotificationType} is not registered, no push message was created for return {ReturnNumber}.",
                jobArgument.NotificationTypeName, orderReturn.Number);

            return null;
        }

        var store = await _storeService.GetNoCloneAsync(orderReturn.StoreId, StoreResponseGroup.StoreInfo.ToString());
        var languageCode = orderReturn.LanguageCode.EmptyToNull() ?? store?.DefaultLanguage;

        returnNotification.ReturnId = orderReturn.Id;
        returnNotification.Return = orderReturn;
        returnNotification.LanguageCode = languageCode;

        if (returnNotification.Templates.FindTemplateForLanguage(languageCode) is not EmailNotificationTemplate template)
        {
            _logger.LogWarning(
                "Notification {NotificationType} has no template for {LanguageCode}, no push message was created for return {ReturnNumber}.",
                jobArgument.NotificationTypeName, languageCode, orderReturn.Number);

            return null;
        }

        return await _templateRenderer.RenderAsync(new NotificationRenderContext
        {
            Template = template.Subject,
            Model = returnNotification,
            Language = template.LanguageCode,
        });
    }

    protected virtual string GetNotificationTypeName(string status)
    {
        return NotificationTypeNamesByStatus.TryGetValue(status ?? string.Empty, out var result) ? result : null;
    }

    protected virtual IReadOnlyDictionary<string, string> NotificationTypeNamesByStatus { get; } = ReturnNotificationTypes.ByStatus;
}
