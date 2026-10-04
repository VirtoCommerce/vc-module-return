using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

// What the email and the push channel share, so that the two behave the same on the same data.
public abstract class ReturnStatusNotificationHandlerBase : IEventHandler<ReturnStatusChangedEvent>
{
    private readonly INotificationSearchService _notificationSearchService;
    private readonly IReturnService _returnService;
    private readonly IReturnSettingsService _settingsService;
    private readonly IStoreService _storeService;
    private readonly IReturnBuyerResolver _buyerResolver;

    protected ReturnStatusNotificationHandlerBase(
        INotificationSearchService notificationSearchService,
        IReturnService returnService,
        IReturnSettingsService settingsService,
        IStoreService storeService,
        IReturnBuyerResolver buyerResolver,
        ILogger logger)
    {
        _notificationSearchService = notificationSearchService;
        _returnService = returnService;
        _settingsService = settingsService;
        _storeService = storeService;
        _buyerResolver = buyerResolver;
        Logger = logger;
    }

    protected ILogger Logger { get; }

    public virtual async Task Handle(ReturnStatusChangedEvent message)
    {
        var orderReturn = message.Return;

        var notificationTypeName = GetNotificationTypeName(message.ToStatus);

        if (notificationTypeName == null || !IsAnnounced(message, notificationTypeName))
        {
            return;
        }

        var rules = await _settingsService.GetRulesAsync(orderReturn.StoreId);

        if (!IsEnabled(rules))
        {
            return;
        }

        if (string.IsNullOrEmpty(orderReturn.CustomerId))
        {
            Logger.LogWarning(
                "Return {ReturnNumber} has no customer, {NotificationType} was not sent.",
                orderReturn.Number, notificationTypeName);

            return;
        }

        var argument = AbstractTypeFactory<ReturnNotificationJobArgument>.TryCreateInstance();

        argument.ReturnId = orderReturn.Id;
        argument.StoreId = orderReturn.StoreId;
        argument.NotificationTypeName = notificationTypeName;

        await EnqueueSending(argument);
    }

    protected abstract bool IsEnabled(ReturnStoreRules rules);

    protected virtual bool IsAnnounced(ReturnStatusChangedEvent message, string notificationTypeName)
    {
        // A draft is the buyer's work in progress, so the only move out of it they need to hear about is
        // the submit. A return created already cancelled was never announced, so nothing is called off.
        if (message.FromStatus.EqualsIgnoreCase(ReturnStatus.Draft))
        {
            return message.ToStatus.EqualsIgnoreCase(ReturnStatus.Requested);
        }

        return message.FromStatus != null ||
               !notificationTypeName.EqualsIgnoreCase(nameof(ReturnCancelledEmailNotification));
    }

    // Out of the save path: a mail server or a push fan-out that is slow or down must not fail the
    // save that a buyer or an agent is waiting on.
    protected abstract Task EnqueueSending(ReturnNotificationJobArgument argument);

    protected virtual async Task<IList<PreparedReturnNotification>> PrepareAsync(IList<ReturnNotificationJobArgument> jobArguments)
    {
        var returnIds = jobArguments.Select(x => x.ReturnId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // A copy this instance cached can predate the change being announced: cached before another instance
        // saved it, or put back by a read that raced the save. Judged on that copy, a decision looked out of
        // date and the buyer heard nothing.
        foreach (var returnId in returnIds)
        {
            GenericCachingRegion<Return>.ExpireTokenForKey(returnId, propagate: false);
        }

        var returnsById = (await _returnService.GetAsync(returnIds, ReturnResponseGroup.None.ToString()))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var result = new List<PreparedReturnNotification>();

        foreach (var jobArgument in jobArguments)
        {
            if (!returnsById.TryGetValue(jobArgument.ReturnId, out var orderReturn))
            {
                Logger.LogWarning(
                    "Return {ReturnId} no longer exists, {NotificationType} was not sent.",
                    jobArgument.ReturnId, jobArgument.NotificationTypeName);

                continue;
            }

            var prepared = await PrepareAsync(jobArgument, orderReturn);

            if (prepared != null)
            {
                result.Add(prepared);
            }
        }

        return result;
    }

    protected virtual async Task<PreparedReturnNotification> PrepareAsync(ReturnNotificationJobArgument jobArgument, Return orderReturn)
    {
        // A move to another announced status queued its own notification, so this one would be old news.
        // A move to a status that announces nothing - Completed, say - leaves this one as the last word.
        var currentTypeName = GetNotificationTypeName(orderReturn.Status);

        if (currentTypeName != null && !jobArgument.NotificationTypeName.EqualsIgnoreCase(currentTypeName))
        {
            Logger.LogInformation(
                "Return {ReturnNumber} is {Status} now, {NotificationType} is out of date and was not sent.",
                orderReturn.Number, orderReturn.Status, jobArgument.NotificationTypeName);

            return null;
        }

        var notification = await _notificationSearchService.GetNotificationAsync(
            jobArgument.NotificationTypeName,
            new TenantIdentity(orderReturn.StoreId, nameof(Store)));

        if (notification is not ReturnEmailNotificationBase returnNotification)
        {
            Logger.LogWarning(
                "Notification {NotificationType} is not registered, return {ReturnNumber} was not announced to the buyer.",
                jobArgument.NotificationTypeName, orderReturn.Number);

            return null;
        }

        // The email sender honours this on its own; checking it here keeps push in step with email.
        if (returnNotification.IsActive != true)
        {
            Logger.LogInformation(
                "Notification {NotificationType} is switched off, return {ReturnNumber} was not announced to the buyer.",
                jobArgument.NotificationTypeName, orderReturn.Number);

            return null;
        }

        var store = await _storeService.GetNoCloneAsync(orderReturn.StoreId, StoreResponseGroup.StoreInfo.ToString());
        var languageCode = orderReturn.LanguageCode.EmptyToNull() ?? store?.DefaultLanguage;

        // The shipped templates carry no language and match every one, so this fails only when they are missing.
        if (returnNotification.Templates.FindTemplateForLanguage(languageCode) is not EmailNotificationTemplate template)
        {
            Logger.LogWarning(
                "Notification {NotificationType} has no template for {LanguageCode}, return {ReturnNumber} was not announced to the buyer.",
                jobArgument.NotificationTypeName, languageCode, orderReturn.Number);

            return null;
        }

        // May be null: email can still reach the buyer through the order's address.
        var buyer = await _buyerResolver.GetBuyerAsync(orderReturn.CustomerId);

        returnNotification.ReturnId = orderReturn.Id;
        returnNotification.Return = orderReturn;
        returnNotification.Customer = buyer?.Member;
        returnNotification.LanguageCode = languageCode;

        var result = AbstractTypeFactory<PreparedReturnNotification>.TryCreateInstance();

        result.Return = orderReturn;
        result.Store = store;
        result.Buyer = buyer;
        result.Notification = returnNotification;
        result.Template = template;

        return result;
    }

    protected virtual string GetNotificationTypeName(string status)
    {
        return NotificationTypeNamesByStatus.TryGetValue(status ?? string.Empty, out var result) ? result : null;
    }

    protected virtual IReadOnlyDictionary<string, string> NotificationTypeNamesByStatus { get; } = ReturnNotificationTypes.ByStatus;
}
