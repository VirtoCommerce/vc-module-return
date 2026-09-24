using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Handlers;

public class SendNotificationsReturnStatusChangedEventHandler : IEventHandler<ReturnStatusChangedEvent>
{
    private readonly INotificationSearchService _notificationSearchService;
    private readonly INotificationSender _notificationSender;
    private readonly IReturnService _returnService;
    private readonly IReturnSettingsService _settingsService;
    private readonly IStoreService _storeService;
    private readonly IMemberService _memberService;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
    private readonly ILogger<SendNotificationsReturnStatusChangedEventHandler> _logger;

    public SendNotificationsReturnStatusChangedEventHandler(
        INotificationSearchService notificationSearchService,
        INotificationSender notificationSender,
        IReturnService returnService,
        IReturnSettingsService settingsService,
        IStoreService storeService,
        IMemberService memberService,
        Func<UserManager<ApplicationUser>> userManagerFactory,
        ILogger<SendNotificationsReturnStatusChangedEventHandler> logger)
    {
        _notificationSearchService = notificationSearchService;
        _notificationSender = notificationSender;
        _returnService = returnService;
        _settingsService = settingsService;
        _storeService = storeService;
        _memberService = memberService;
        _userManagerFactory = userManagerFactory;
        _logger = logger;
    }

    public virtual async Task Handle(ReturnStatusChangedEvent message)
    {
        var orderReturn = message.Return;

        var notificationTypeName = GetNotificationTypeName(message.ToStatus);

        // Every other transition - a draft being created, a cancellation, a status this iteration
        // does not model - is silent by design rather than by omission.
        if (notificationTypeName == null)
        {
            return;
        }

        var rules = await _settingsService.GetRulesAsync(orderReturn.StoreId);

        if (!rules.SendNotifications)
        {
            return;
        }

        var argument = new ReturnNotificationJobArgument
        {
            ReturnId = orderReturn.Id,
            StoreId = orderReturn.StoreId,
            CustomerId = orderReturn.CustomerId,
            NotificationTypeName = notificationTypeName,
            NotifyOrganization = rules.NotifyOrganizationEmail && !string.IsNullOrEmpty(orderReturn.OrganizationId),
        };

        EnqueueSending(argument);
    }

    /// <summary>
    /// Out of the save path: an SMTP server that is slow or down must not fail the save that a
    /// buyer or an agent is waiting on.
    /// </summary>
    protected virtual void EnqueueSending(ReturnNotificationJobArgument argument)
    {
        BackgroundJob.Enqueue<SendNotificationsReturnStatusChangedEventHandler>(x => x.SendNotificationsAsync(new[] { argument }));
    }

    public virtual async Task SendNotificationsAsync(ReturnNotificationJobArgument[] jobArguments)
    {
        var returnsById = (await _returnService.GetAsync(
                jobArguments.Select(x => x.ReturnId).Distinct().ToList(),
                ReturnResponseGroup.None.ToString()))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var jobArgument in jobArguments)
        {
            if (!returnsById.TryGetValue(jobArgument.ReturnId, out var orderReturn))
            {
                continue;
            }

            var store = await _storeService.GetNoCloneAsync(orderReturn.StoreId, StoreResponseGroup.StoreInfo.ToString());
            var customer = await GetCustomerAsync(jobArgument.CustomerId);
            var customerEmail = await GetRecipientEmailAsync(jobArgument.CustomerId, customer);

            if (string.IsNullOrEmpty(customerEmail))
            {
                _logger.LogWarning(
                    "No email address for customer {CustomerId}, return {ReturnNumber} was not announced to the buyer.",
                    jobArgument.CustomerId, orderReturn.Number);
            }
            else
            {
                await SendAsync(jobArgument, orderReturn, store, customer, customerEmail);
            }

            if (!jobArgument.NotifyOrganization)
            {
                continue;
            }

            var organizationEmail = await GetOrganizationEmailAsync(orderReturn.OrganizationId);

            if (string.IsNullOrEmpty(organizationEmail))
            {
                _logger.LogWarning(
                    "No email address for organization {OrganizationId}, return {ReturnNumber} was not copied to it.",
                    orderReturn.OrganizationId, orderReturn.Number);
            }
            // A buyer whose own address is the organization's would get the same email twice.
            else if (!organizationEmail.EqualsIgnoreCase(customerEmail))
            {
                await SendAsync(jobArgument, orderReturn, store, customer, organizationEmail);
            }
        }
    }

    /// <summary>
    /// The organization's copy is the buyer's own email, sent to another address: purchasing wants
    /// to see exactly what the buyer was told.
    /// </summary>
    protected virtual async Task SendAsync(ReturnNotificationJobArgument jobArgument, Return orderReturn, Store store, Member customer, string recipientEmail)
    {
        var notification = await _notificationSearchService.GetNotificationAsync(
            jobArgument.NotificationTypeName,
            new TenantIdentity(jobArgument.StoreId, nameof(Store)));

        if (notification is not ReturnEmailNotificationBase returnNotification)
        {
            _logger.LogWarning(
                "Notification {NotificationType} is not registered, return {ReturnNumber} was not announced to {Recipient}.",
                jobArgument.NotificationTypeName, orderReturn.Number, recipientEmail);

            return;
        }

        returnNotification.ReturnId = orderReturn.Id;
        returnNotification.Return = orderReturn;
        returnNotification.Customer = customer;
        returnNotification.LanguageCode = orderReturn.LanguageCode.EmptyToNull() ?? store?.DefaultLanguage;
        returnNotification.From = store?.EmailWithName;
        returnNotification.To = recipientEmail;
        returnNotification.TenantIdentity = new TenantIdentity(orderReturn.Id, nameof(Return));

        await _notificationSender.ScheduleSendNotificationAsync(returnNotification);
    }

    protected virtual async Task<string> GetOrganizationEmailAsync(string organizationId)
    {
        if (string.IsNullOrEmpty(organizationId))
        {
            return null;
        }

        var organization = await _memberService.GetByIdAsync(organizationId);

        return organization?.Emails?.FirstOrDefault(x => !string.IsNullOrEmpty(x));
    }

    protected virtual string GetNotificationTypeName(string status)
    {
        return NotificationTypeNamesByStatus.TryGetValue(status ?? string.Empty, out var result) ? result : null;
    }

    protected virtual IReadOnlyDictionary<string, string> NotificationTypeNamesByStatus { get; } = ReturnNotificationTypes.ByStatus;

    protected virtual async Task<string> GetRecipientEmailAsync(string customerId, Member customer)
    {
        var email = customer?.Emails?.FirstOrDefault(x => !string.IsNullOrEmpty(x));

        if (!string.IsNullOrEmpty(email))
        {
            return email;
        }

        using var userManager = _userManagerFactory();
        var user = await userManager.FindByIdAsync(customerId);

        return user?.Email;
    }

    protected virtual async Task<Member> GetCustomerAsync(string customerId)
    {
        if (string.IsNullOrEmpty(customerId))
        {
            return null;
        }

        var result = await _memberService.GetByIdAsync(customerId);

        if (result == null)
        {
            using var userManager = _userManagerFactory();
            var user = await userManager.FindByIdAsync(customerId);

            if (user?.MemberId != null)
            {
                result = await _memberService.GetByIdAsync(user.MemberId);
            }
        }

        return result;
    }
}

public class ReturnNotificationJobArgument
{
    public string ReturnId { get; set; }

    public string StoreId { get; set; }

    public string CustomerId { get; set; }

    public string NotificationTypeName { get; set; }

    /// <summary>
    /// Decided when the status changed, as whether to notify at all is, so that the store setting
    /// in force at that moment is the one that counts.
    /// </summary>
    public bool NotifyOrganization { get; set; }
}
