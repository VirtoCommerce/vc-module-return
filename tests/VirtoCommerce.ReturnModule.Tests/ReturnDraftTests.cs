using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.ReturnModule.Data.Validation;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnDraftTests
{
    private const string BuyerId = "buyer-1";
    private const string OrderId = "order-1";
    private const string OrderLineItemId = "order-line-1";

    private readonly Mock<ICustomerOrderService> _orderService = new();
    private readonly Mock<IReturnService> _returnService = new();
    private readonly Mock<IReturnEligibilityService> _eligibilityService = new();
    private readonly Mock<IReturnAttachmentService> _attachmentService = new();
    private readonly Mock<IReturnSettingsService> _settingsService = new();
    private readonly List<Return> _saved = [];

    public ReturnDraftTests()
    {
        _orderService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new CustomerOrder { Id = OrderId, CustomerId = BuyerId, StoreId = "B2B-store", Items = [new LineItem { Id = OrderLineItemId, Quantity = 2 }] }]);

        _eligibilityService
            .Setup(x => x.GetOrderEligibility(It.IsAny<CustomerOrder>()))
            .ReturnsAsync(ReturnEligibility.Eligible());

        _attachmentService
            .Setup(x => x.ValidateAvailable(It.IsAny<Return>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Task.CompletedTask);

        _settingsService
            .Setup(x => x.GetRulesAsync(It.IsAny<string>()))
            .ReturnsAsync(new ReturnStoreRules());

        _returnService
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<Return>>()))
            .Callback<IList<Return>>(_saved.AddRange)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateDraft_SnapshotsTheCultureTheBuyerIsUsing()
    {
        // The status emails are written in it later, whatever language the order was placed in.
        await CreateDraft(new ReturnFlowService(_orderService.Object, _returnService.Object, _eligibilityService.Object, _attachmentService.Object, new ReturnStateProvider(), _settingsService.Object, new ReturnRequestValidator(), Mock.Of<IReturnQuantityService>()), "de-DE");

        Assert.Equal("de-DE", Assert.Single(_saved).LanguageCode);
    }

    [Fact]
    public async Task CreateDraft_CultureLongerThanItsColumn_IsRefused()
    {
        var service = new ReturnFlowService(_orderService.Object, _returnService.Object, _eligibilityService.Object, _attachmentService.Object, new ReturnStateProvider(), _settingsService.Object, new ReturnRequestValidator(), Mock.Of<IReturnQuantityService>());

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => CreateDraft(service, "en-US-x-a-very-long-subtag"));

        Assert.Equal(ReturnFlowError.InvalidRequest, exception.Code);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task CreateDraft_StillRunsAnOverrideOfTheReleasedValidation()
    {
        // 3.1002.0 shipped ValidateRequestAsync with five parameters; a service built on it must keep
        // being asked when a draft is created.
        var service = new RefusingEverythingService(_orderService.Object, _returnService.Object, _eligibilityService.Object, _attachmentService.Object, _settingsService.Object);

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => CreateDraft(service, "de-DE"));

        Assert.Equal(ReturnFlowError.WrongStatus, exception.Code);
        Assert.Empty(_saved);
    }

    private sealed class RefusingEverythingService : ReturnFlowService
    {
        public RefusingEverythingService(
            ICustomerOrderService orderService,
            IReturnService returnService,
            IReturnEligibilityService eligibilityService,
            IReturnAttachmentService attachmentService,
            IReturnSettingsService settingsService)
            : base(orderService, returnService, eligibilityService, attachmentService, new ReturnStateProvider(), settingsService, new ReturnRequestValidator(), quantityService: null)
        {
        }

        protected override Task ValidateRequestAsync(string storeId, string customerReference, string customerComment, IList<CreateReturnItemRequest> items, bool requireReason = false)
        {
            throw new ReturnFlowException(ReturnFlowError.WrongStatus, "Refused by the override.");
        }
    }

    private static Task<Return> CreateDraft(ReturnFlowService service, string cultureName)
    {
        var request = new CreateReturnRequest
        {
            OrderId = OrderId,
            Items = [new CreateReturnItemRequest { OrderLineItemId = OrderLineItemId, Quantity = 1 }],
        };

        var context = new ReturnFlowContext { CustomerId = BuyerId, LanguageCode = cultureName };

        return service.CreateDraft(request, context, TestContext.Current.CancellationToken);
    }
}
