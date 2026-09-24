using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

/// <summary>
/// What the email and the push channel share: which transitions are announced, whether the store
/// wants them, and - when the job runs - who the buyer is and which template speaks to them. Kept in
/// one place so that the two channels behave the same on the same data.
/// </summary>
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

        // Every other transition - a draft being created, a status this iteration does not model -
        // is silent by design rather than by omission.
        if (notificationTypeName == null)
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

        EnqueueSending(argument);
    }

    protected abstract bool IsEnabled(ReturnStoreRules rules);

    /// <summary>
    /// Out of the save path: a mail server or a push fan-out that is slow or down must not fail the
    /// save that a buyer or an agent is waiting on.
    /// </summary>
    protected abstract void EnqueueSending(ReturnNotificationJobArgument argument);

    /// <summary>
    /// Re-reads each return and keeps only the jobs that can still be delivered as queued. Whatever
    /// is dropped is logged with the reason.
    /// </summary>
    protected virtual async Task<IList<PreparedReturnNotification>> PrepareAsync(IList<ReturnNotificationJobArgument> jobArguments)
    {
        var returnsById = (await _returnService.GetAsync(
                jobArguments.Select(x => x.ReturnId).Distinct().ToList(),
                ReturnResponseGroup.None.ToString()))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var result = new List<PreparedReturnNotification>();

        foreach (var jobArgument in jobArguments)
        {
            if (!returnsById.TryGetValue(jobArgument.ReturnId, out var orderReturn))
            {
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
        // The return moved on before the job ran. The move published its own event, so sending this
        // one would only put the old news next to a body rendered from the new state.
        if (!jobArgument.NotificationTypeName.EqualsIgnoreCase(GetNotificationTypeName(orderReturn.Status)))
        {
            Logger.LogInformation(
                "Return {ReturnNumber} is {Status} now, {NotificationType} is out of date and was not sent.",
                orderReturn.Number, orderReturn.Status, jobArgument.NotificationTypeName);

            return null;
        }

        var notification = await _notificationSearchService.GetNotificationAsync(
            jobArgument.NotificationTypeName,
            new TenantIdentity(jobArgument.StoreId, nameof(Store)));

        if (notification is not ReturnEmailNotificationBase returnNotification)
        {
            Logger.LogWarning(
                "Notification {NotificationType} is not registered, return {ReturnNumber} was not announced to the buyer.",
                jobArgument.NotificationTypeName, orderReturn.Number);

            return null;
        }

        var store = await _storeService.GetNoCloneAsync(orderReturn.StoreId, StoreResponseGroup.StoreInfo.ToString());
        var languageCode = orderReturn.LanguageCode.EmptyToNull() ?? store?.DefaultLanguage;

        // A template saved in the admin for some languages only matches none of the others, and
        // rendering without one produces an empty subject and body rather than an error.
        if (returnNotification.Templates.FindTemplateForLanguage(languageCode) is not EmailNotificationTemplate template)
        {
            Logger.LogWarning(
                "Notification {NotificationType} has no template for {LanguageCode}, return {ReturnNumber} was not announced to the buyer.",
                jobArgument.NotificationTypeName, languageCode, orderReturn.Number);

            return null;
        }

        var buyer = await _buyerResolver.GetBuyerAsync(orderReturn.CustomerId);

        if (buyer == null)
        {
            Logger.LogWarning(
                "Customer {CustomerId} was not found, return {ReturnNumber} was not announced to the buyer.",
                orderReturn.CustomerId, orderReturn.Number);

            return null;
        }

        returnNotification.ReturnId = orderReturn.Id;
        returnNotification.Return = orderReturn;
        returnNotification.Customer = buyer.Member;
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
