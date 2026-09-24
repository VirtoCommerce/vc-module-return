using System;
using System.Collections.Generic;
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
using VirtoCommerce.PushMessages.Core.Models;
using VirtoCommerce.PushMessages.Core.Services;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Handlers;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnPushMessageHandlerTests
{
    private const string StoreId = "B2B-store";
    // A storefront return carries the buyer's user id; PushMessages addresses the contact behind it.
    private const string CustomerId = "user-1";
    private const string ContactId = "contact-1";
    private const string ReturnId = "return-1";
    private const string ReturnNumber = "RET260922-00001";

    private readonly Mock<INotificationSearchService> _notificationSearchService = new();
    private readonly Mock<INotificationTemplateRenderer> _templateRenderer = new();
    private readonly Mock<IPushMessageService> _pushMessageService = new();
    private readonly Mock<IReturnService> _returnService = new();
    private readonly Mock<IReturnSettingsService> _settingsService = new();
    private readonly Mock<IStoreService> _storeService = new();
    private readonly Mock<IMemberService> _memberService = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager =
        new(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

    private readonly List<PushMessage> _saved = [];
    private readonly ReturnStoreRules _rules = new() { SendPushNotifications = true };
    private readonly Store _store = new() { Id = StoreId, DefaultLanguage = "nl-NL" };

    private Return _orderReturn = NewReturn();
    private string _templateLanguageCode = "de-DE";
    private string _subject = "Return {{ return.number }} received";
    private bool _notificationIsActive = true;

    public ReturnPushMessageHandlerTests()
    {
        _settingsService.Setup(x => x.GetRulesAsync(It.IsAny<string>())).ReturnsAsync(() => _rules);

        _storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [_store]);

        _returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [_orderReturn]);

        _userManager
            .Setup(x => x.FindByIdAsync(CustomerId))
            .ReturnsAsync(new ApplicationUser { Id = CustomerId, MemberId = ContactId });

        _memberService
            .Setup(x => x.GetByIdAsync(ContactId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => new Contact { Id = ContactId, Name = "Jan de Vries" });

        _notificationSearchService
            .Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>()))
            .ReturnsAsync(() => new NotificationSearchResult
            {
                Results = [NewNotification()],
                TotalCount = 1,
            });

        _templateRenderer
            .Setup(x => x.RenderAsync(It.IsAny<NotificationRenderContext>()))
            .ReturnsAsync((NotificationRenderContext context) =>
                context.Template.Replace("{{ return.number }}", ((ReturnEmailNotificationBase)context.Model).Return.Number));

        _pushMessageService
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<PushMessage>>()))
            .Callback<IList<PushMessage>>(_saved.AddRange)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task AnnouncedStatus_CreatesOneSentMessageForTheBuyer()
    {
        await HandleAndSend(ReturnStatus.Requested);

        var pushMessage = Assert.Single(_saved);

        Assert.Equal(ReturnNumber, pushMessage.Topic);
        Assert.Equal($"Return {ReturnNumber} received", pushMessage.ShortMessage);
        Assert.Equal(PushMessageStatus.Sent, pushMessage.Status);
        Assert.Equal([ContactId], pushMessage.MemberIds);
    }

    [Fact]
    public async Task SubjectFileNewline_IsNotKeptInTheMessage()
    {
        _subject = "Return {{ return.number }} received" + Environment.NewLine;

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Equal($"Return {ReturnNumber} received", Assert.Single(_saved).ShortMessage);
    }

    [Fact]
    public async Task NotificationSwitchedOff_CreatesNothing_AsForTheEmail()
    {
        _notificationIsActive = false;

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(_saved);
    }

    [Fact]
    public async Task BuyerNotFound_CreatesNothing()
    {
        _userManager.Setup(x => x.FindByIdAsync(CustomerId)).ReturnsAsync((ApplicationUser)null);
        _memberService
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Member)null);

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(_saved);
    }

    [Fact]
    public async Task CustomerIdIsAMemberId_AddressesThatMember()
    {
        _orderReturn = NewReturn(customerId: ContactId);

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Equal([ContactId], Assert.Single(_saved).MemberIds);
    }

    [Fact]
    public async Task LoginWithoutContact_CreatesNothing()
    {
        _userManager
            .Setup(x => x.FindByIdAsync(CustomerId))
            .ReturnsAsync(new ApplicationUser { Id = CustomerId, Email = "login@aras.example" });

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(_saved);
    }

    [Fact]
    public async Task ReturnMovedOnBeforeTheJobRan_CreatesNothing()
    {
        var handler = NewHandler();
        _orderReturn.Status = ReturnStatus.Requested;

        await handler.Handle(new ReturnStatusChangedEvent(_orderReturn, ReturnStatus.Draft, ReturnStatus.Requested));
        _orderReturn.Status = ReturnStatus.Cancelled;
        await handler.SendPushMessagesAsync([.. handler.Enqueued]);

        Assert.Empty(_saved);
    }

    [Theory]
    [InlineData(ReturnStatus.Draft)]
    [InlineData(ReturnStatus.Processing)]
    [InlineData(null)]
    public async Task StatusWithNoTemplate_QueuesNothing(string status)
    {
        var handler = await HandleAndSend(status);

        Assert.Empty(handler.Enqueued);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task PushNotificationsDisabledForStore_QueuesNothing()
    {
        _rules.SendPushNotifications = false;

        var handler = await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(handler.Enqueued);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task ReturnWithoutCustomer_QueuesNothing()
    {
        _orderReturn = NewReturn(customerId: null);

        var handler = await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(handler.Enqueued);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task TextIsRenderedInTheCultureTheReturnWasRaisedIn()
    {
        await HandleAndSend(ReturnStatus.Requested);

        _templateRenderer.Verify(
            x => x.RenderAsync(It.Is<NotificationRenderContext>(context => context.Language == "de-DE")),
            Times.Once);
    }

    [Fact]
    public async Task NoTemplateForTheLanguage_CreatesNothingRatherThanThrowing()
    {
        _templateLanguageCode = "fr-FR";
        _orderReturn = NewReturn(languageCode: "ja-JP");
        _store.DefaultLanguage = "ja-JP";

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(_saved);
    }

    [Fact]
    public async Task NotificationTypeNotRegistered_CreatesNothingRatherThanThrowing()
    {
        _notificationSearchService
            .Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>()))
            .ReturnsAsync(new NotificationSearchResult { Results = [], TotalCount = 0 });

        await HandleAndSend(ReturnStatus.Requested);

        Assert.Empty(_saved);
    }

    private async Task<TestableHandler> HandleAndSend(string status)
    {
        _orderReturn.Status = status;

        var handler = NewHandler();

        await handler.Handle(new ReturnStatusChangedEvent(_orderReturn, ReturnStatus.Requested, status));

        if (handler.Enqueued.Count > 0)
        {
            await handler.SendPushMessagesAsync([.. handler.Enqueued]);
        }

        return handler;
    }

    private TestableHandler NewHandler()
    {
        return new TestableHandler(
            _notificationSearchService.Object,
            _templateRenderer.Object,
            _pushMessageService.Object,
            _returnService.Object,
            _settingsService.Object,
            _storeService.Object,
            new ReturnBuyerResolver(_memberService.Object, () => _userManager.Object),
            NullLogger<SendPushMessagesReturnStatusChangedEventHandler>.Instance);
    }

    private static Return NewReturn(string languageCode = "de-DE", string customerId = CustomerId)
    {
        return new Return
        {
            Id = ReturnId,
            Number = ReturnNumber,
            StoreId = StoreId,
            CustomerId = customerId,
            LanguageCode = languageCode,
            Status = ReturnStatus.Requested,
        };
    }

    private Notification NewNotification()
    {
        var result = new ReturnRegisteredEmailNotification { IsActive = _notificationIsActive };

        result.Templates.Add(new EmailNotificationTemplate
        {
            LanguageCode = _templateLanguageCode,
            Subject = _subject,
        });

        return result;
    }

    private sealed class TestableHandler : SendPushMessagesReturnStatusChangedEventHandler
    {
        public TestableHandler(
            INotificationSearchService notificationSearchService,
            INotificationTemplateRenderer templateRenderer,
            IPushMessageService pushMessageService,
            IReturnService returnService,
            IReturnSettingsService settingsService,
            IStoreService storeService,
            IReturnBuyerResolver buyerResolver,
            ILogger<SendPushMessagesReturnStatusChangedEventHandler> logger)
            : base(notificationSearchService, templateRenderer, pushMessageService, returnService, settingsService, storeService, buyerResolver, logger)
        {
        }

        public List<ReturnNotificationJobArgument> Enqueued { get; } = [];

        protected override void EnqueueSending(ReturnNotificationJobArgument argument)
        {
            Enqueued.Add(argument);
        }
    }
}
