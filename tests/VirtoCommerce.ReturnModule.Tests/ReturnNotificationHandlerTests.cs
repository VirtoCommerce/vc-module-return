using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Handlers;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnNotificationHandlerTests
{
    private const string StoreId = "B2B-store";
    private const string CustomerId = "contact-1";
    private const string ReturnId = "return-1";
    private const string OrganizationId = "org-1";

    private readonly Mock<INotificationSearchService> _notificationSearchService = new();
    private readonly Mock<INotificationSender> _notificationSender = new();
    private readonly Mock<IReturnService> _returnService = new();
    private readonly Mock<IReturnSettingsService> _settingsService = new();
    private readonly Mock<IStoreService> _storeService = new();
    private readonly Mock<IMemberService> _memberService = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager =
        new(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

    private readonly List<Notification> _sent = [];
    private readonly ReturnStoreRules _rules = new() { SendNotifications = true };
    private readonly Store _store = new() { Id = StoreId, Email = "returns@aras.example", DefaultLanguage = "nl-NL" };

    private Return _orderReturn = NewReturn();

    public ReturnNotificationHandlerTests()
    {
        _settingsService.Setup(x => x.GetRulesAsync(It.IsAny<string>())).ReturnsAsync(() => _rules);
        // GetNoCloneAsync is an extension over the CRUD contract, so the mock answers what it calls.
        _storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [_store]);

        _returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [_orderReturn]);

        _memberService
            .Setup(x => x.GetByIdAsync(CustomerId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => new Contact { Id = CustomerId, Name = "Jan de Vries", Emails = ["jan@aras.example"] });

        _memberService
            .Setup(x => x.GetByIdAsync(OrganizationId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => new Organization { Id = OrganizationId, Name = "ARAS Security BV", Emails = ["purchasing@aras.example"] });

        _notificationSearchService
            .Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>()))
            .ReturnsAsync((NotificationSearchCriteria criteria) => new NotificationSearchResult
            {
                Results = [NewNotification(criteria.NotificationType)],
                TotalCount = 1,
            });

        _notificationSender
            .Setup(x => x.ScheduleSendNotificationAsync(It.IsAny<Notification>()))
            .Callback<Notification>(_sent.Add)
            .Returns(Task.CompletedTask);
    }

    [Theory]
    [InlineData(ReturnStatus.Requested, nameof(ReturnRegisteredEmailNotification))]
    [InlineData(ReturnStatus.Approved, nameof(ReturnApprovedEmailNotification))]
    [InlineData(ReturnStatus.PartiallyApproved, nameof(ReturnPartiallyApprovedEmailNotification))]
    [InlineData(ReturnStatus.Rejected, nameof(ReturnRejectedEmailNotification))]
    [InlineData(ReturnStatus.Cancelled, nameof(ReturnCancelledEmailNotification))]
    [InlineData("Canceled", nameof(ReturnCancelledEmailNotification))]
    public async Task EachAnnouncedStatus_SendsItsOwnNotification(string status, string expectedType)
    {
        await HandleAndSend(status);

        var notification = Assert.Single(_sent);
        Assert.Equal(expectedType, notification.Type);
    }

    [Theory]
    [InlineData(ReturnStatus.Draft)]
    [InlineData(ReturnStatus.Processing)]
    [InlineData(ReturnStatus.Completed)]
    [InlineData(null)]
    public async Task StatusWithNoTemplate_QueuesNothing(string status)
    {
        var handler = await HandleAndSend(status);

        Assert.Empty(handler.Enqueued);
        Assert.Empty(_sent);
    }

    [Fact]
    public async Task NotificationsDisabledForStore_QueuesNothing()
    {
        _rules.SendNotifications = false;

        var handler = await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(handler.Enqueued);
        Assert.Empty(_sent);
    }

    [Fact]
    public async Task SentNotification_CarriesSenderRecipientAndReturn()
    {
        await HandleAndSend(ReturnStatus.Requested);

        var notification = Assert.IsAssignableFrom<ReturnEmailNotificationBase>(Assert.Single(_sent));

        Assert.Equal("jan@aras.example", notification.To);
        Assert.Equal(_store.EmailWithName, notification.From);
        Assert.Equal(ReturnId, notification.ReturnId);
        Assert.Equal("RET260922-00001", notification.Return.Number);
        Assert.Equal("Jan de Vries", notification.Customer.Name);
    }

    [Fact]
    public async Task SentNotification_UsesTheCultureTheReturnWasRaisedIn()
    {
        await HandleAndSend(ReturnStatus.Requested);

        Assert.Equal("de-DE", Assert.Single(_sent).LanguageCode);
    }

    [Fact]
    public async Task ReturnWithoutLanguage_FallsBackToTheStoreDefault()
    {
        _orderReturn = NewReturn(languageCode: null);

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Equal("nl-NL", Assert.Single(_sent).LanguageCode);
    }

    [Fact]
    public async Task ContactWithoutEmail_SendsNothingRatherThanThrowing()
    {
        _memberService
            .Setup(x => x.GetByIdAsync(CustomerId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => new Contact { Id = CustomerId, Name = "Jan de Vries", Emails = [] });

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(_sent);
    }

    [Fact]
    public async Task NotificationTypeNotRegistered_SendsNothingRatherThanThrowing()
    {
        _notificationSearchService
            .Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>()))
            .ReturnsAsync(new NotificationSearchResult { Results = [], TotalCount = 0 });

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(_sent);
    }

    [Fact]
    public async Task OrganizationCopyEnabled_SendsTheSameEmailToTheOrganization()
    {
        _rules.NotifyOrganizationEmail = true;
        _orderReturn.OrganizationId = OrganizationId;

        await HandleAndSend(ReturnStatus.Approved);

        Assert.Equal(["jan@aras.example", "purchasing@aras.example"], _sent.Select(x => ((EmailNotification)x).To));
        Assert.All(_sent, x => Assert.Equal(nameof(ReturnApprovedEmailNotification), x.Type));
    }

    [Fact]
    public async Task OrganizationCopyDisabled_SendsOnlyToTheBuyer()
    {
        _orderReturn.OrganizationId = OrganizationId;

        await HandleAndSend(ReturnStatus.Approved);

        Assert.Equal("jan@aras.example", ((EmailNotification)Assert.Single(_sent)).To);
    }

    [Fact]
    public async Task OrganizationCopyEnabled_ReturnWithoutOrganization_SendsOnlyToTheBuyer()
    {
        _rules.NotifyOrganizationEmail = true;

        var handler = await HandleAndSend(ReturnStatus.Approved);

        Assert.False(Assert.Single(handler.Enqueued).NotifyOrganization);
        Assert.Equal("jan@aras.example", ((EmailNotification)Assert.Single(_sent)).To);
    }

    [Fact]
    public async Task OrganizationSharesTheBuyersAddress_SendsItOnce()
    {
        _rules.NotifyOrganizationEmail = true;
        _orderReturn.OrganizationId = OrganizationId;
        _memberService
            .Setup(x => x.GetByIdAsync(OrganizationId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => new Organization { Id = OrganizationId, Emails = ["JAN@aras.example"] });

        await HandleAndSend(ReturnStatus.Approved);

        Assert.Single(_sent);
    }

    [Fact]
    public async Task BuyerWithoutEmail_StillCopiesTheOrganization()
    {
        _rules.NotifyOrganizationEmail = true;
        _orderReturn.OrganizationId = OrganizationId;
        _memberService
            .Setup(x => x.GetByIdAsync(CustomerId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => new Contact { Id = CustomerId, Name = "Jan de Vries", Emails = [] });

        await HandleAndSend(ReturnStatus.Approved);

        Assert.Equal("purchasing@aras.example", ((EmailNotification)Assert.Single(_sent)).To);
    }

    [Fact]
    public async Task OrganizationWithoutEmail_SendsOnlyToTheBuyer()
    {
        _rules.NotifyOrganizationEmail = true;
        _orderReturn.OrganizationId = OrganizationId;
        _memberService
            .Setup(x => x.GetByIdAsync(OrganizationId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => new Organization { Id = OrganizationId, Emails = [] });

        await HandleAndSend(ReturnStatus.Approved);

        Assert.Equal("jan@aras.example", ((EmailNotification)Assert.Single(_sent)).To);
    }

    /// <summary>
    /// Drives the whole path the runtime takes: the event decides what to queue, the job sends it.
    /// </summary>
    private async Task<TestableHandler> HandleAndSend(string status)
    {
        _orderReturn.Status = status;

        var handler = NewHandler();

        await handler.Handle(new ReturnStatusChangedEvent(_orderReturn, ReturnStatus.Requested, status));

        if (handler.Enqueued.Count > 0)
        {
            await handler.SendNotificationsAsync([.. handler.Enqueued]);
        }

        return handler;
    }

    private TestableHandler NewHandler()
    {
        return new TestableHandler(
            _notificationSearchService.Object,
            _notificationSender.Object,
            _returnService.Object,
            _settingsService.Object,
            _storeService.Object,
            _memberService.Object,
            () => _userManager.Object,
            NullLogger<SendNotificationsReturnStatusChangedEventHandler>.Instance);
    }

    private static Return NewReturn(string languageCode = "de-DE")
    {
        return new Return
        {
            Id = ReturnId,
            Number = "RET260922-00001",
            StoreId = StoreId,
            CustomerId = CustomerId,
            LanguageCode = languageCode,
            Status = ReturnStatus.Requested,
        };
    }

    private static Notification NewNotification(string type)
    {
        return type switch
        {
            nameof(ReturnRegisteredEmailNotification) => new ReturnRegisteredEmailNotification(),
            nameof(ReturnApprovedEmailNotification) => new ReturnApprovedEmailNotification(),
            nameof(ReturnPartiallyApprovedEmailNotification) => new ReturnPartiallyApprovedEmailNotification(),
            nameof(ReturnRejectedEmailNotification) => new ReturnRejectedEmailNotification(),
            nameof(ReturnCancelledEmailNotification) => new ReturnCancelledEmailNotification(),
            _ => throw new InvalidOperationException($"Unexpected notification type '{type}'."),
        };
    }

    // Hangfire needs a configured storage to enqueue, and what matters here is what would have been
    // enqueued rather than that Hangfire works.
    private sealed class TestableHandler : SendNotificationsReturnStatusChangedEventHandler
    {
        public TestableHandler(
            INotificationSearchService notificationSearchService,
            INotificationSender notificationSender,
            IReturnService returnService,
            IReturnSettingsService settingsService,
            IStoreService storeService,
            IMemberService memberService,
            Func<UserManager<ApplicationUser>> userManagerFactory,
            ILogger<SendNotificationsReturnStatusChangedEventHandler> logger)
            : base(notificationSearchService, notificationSender, returnService, settingsService, storeService, memberService, userManagerFactory, logger)
        {
        }

        public List<ReturnNotificationJobArgument> Enqueued { get; } = [];

        protected override void EnqueueSending(ReturnNotificationJobArgument argument)
        {
            Enqueued.Add(argument);
        }
    }
}
