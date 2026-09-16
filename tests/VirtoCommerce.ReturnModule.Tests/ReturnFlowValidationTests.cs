using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnFlowValidationTests
{
    private const string LineId = "line-1";

    [Fact]
    public void DuplicateLines_AreRejected()
    {
        var service = CreateService();

        var exception = Assert.Throws<ReturnFlowException>(() =>
            service.ValidateLines(
            [
                new CreateReturnItemRequest { OrderLineItemId = LineId, Quantity = 5 },
                new CreateReturnItemRequest { OrderLineItemId = LineId, Quantity = 5 },
            ]));

        Assert.Equal(ReturnFlowError.DuplicateLine, exception.Code);
    }

    [Fact]
    public void DuplicateLines_DifferingOnlyInCase_AreRejected()
    {
        var service = CreateService();

        var exception = Assert.Throws<ReturnFlowException>(() =>
            service.ValidateLines(
            [
                new CreateReturnItemRequest { OrderLineItemId = "line-1", Quantity = 1 },
                new CreateReturnItemRequest { OrderLineItemId = "LINE-1", Quantity = 1 },
            ]));

        Assert.Equal(ReturnFlowError.DuplicateLine, exception.Code);
    }

    [Fact]
    public void DistinctLines_AreAccepted()
    {
        var service = CreateService();

        service.ValidateLines(
        [
            new CreateReturnItemRequest { OrderLineItemId = "line-1", Quantity = 1 },
            new CreateReturnItemRequest { OrderLineItemId = "line-2", Quantity = 1 },
        ]);
    }

    [Fact]
    public async Task Availability_IsSummedPerOrderLine_NotCheckedRowByRow()
    {
        // Two rows of 5 against 5 available each pass on their own; together they claim 10 of 5.
        var service = CreateService(returnableQuantity: 5);

        var orderReturn = new Return
        {
            LineItems =
            [
                new ReturnLineItem { OrderLineItemId = LineId, Quantity = 5 },
                new ReturnLineItem { OrderLineItemId = LineId, Quantity = 5 },
            ],
        };

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.Validate(orderReturn, new CustomerOrder()));

        Assert.Equal(ReturnFlowError.QuantityUnavailable, exception.Code);
    }

    [Fact]
    public async Task EveryReasonTheStoreOffers_IsAccepted()
    {
        // Return.Reasons is a dictionary setting, so its items come from the localization store and
        // not from the setting's own value - reading the latter would pass only the default one.
        var service = CreateServiceWithReasons("FaultyOnArrival", "DamagedInTransit", "NoLongerNeeded");

        await service.ValidateRequest(
            "store-1",
            [new CreateReturnItemRequest { OrderLineItemId = LineId, Quantity = 1, ReasonCode = "NoLongerNeeded" }]);
    }

    [Fact]
    public async Task ReasonTheStoreDoesNotOffer_IsRefused()
    {
        var service = CreateServiceWithReasons("FaultyOnArrival");

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.ValidateRequest(
                "store-1",
                [new CreateReturnItemRequest { OrderLineItemId = LineId, Quantity = 1, ReasonCode = "NoLongerNeeded" }]));

        Assert.Equal(ReturnFlowError.InvalidRequest, exception.Code);
    }

    [Fact]
    public async Task Availability_WithinTheLimit_Passes()
    {
        var service = CreateService(returnableQuantity: 5);

        var orderReturn = new Return
        {
            LineItems = [new ReturnLineItem { OrderLineItemId = LineId, Quantity = 5 }],
        };

        await service.Validate(orderReturn, new CustomerOrder());
    }

    private static TestableReturnFlowService CreateServiceWithReasons(params string[] reasons)
    {
        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new Store { Id = "store-1" }]);

        var localizableSettingService = new Mock<ILocalizableSettingService>();
        localizableSettingService
            .Setup(x => x.GetValuesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(reasons.Select(x => new KeyValue { Key = x, Value = x }).ToList());

        return new TestableReturnFlowService(new Mock<IReturnEligibilityService>().Object, storeService.Object, localizableSettingService.Object);
    }

    private static TestableReturnFlowService CreateService(int returnableQuantity = 0)
    {
        var eligibilityService = new Mock<IReturnEligibilityService>();
        eligibilityService
            .Setup(x => x.GetReturnableItems(It.IsAny<CustomerOrder>(), It.IsAny<string>()))
            .ReturnsAsync(() =>
            [
                new ReturnableItem
                {
                    OrderLineItemId = LineId,
                    IsReturnable = true,
                    ReturnableQuantity = returnableQuantity,
                },
            ]);

        return new TestableReturnFlowService(eligibilityService.Object);
    }

    private sealed class TestableReturnFlowService : ReturnFlowService
    {
        public TestableReturnFlowService(
            IReturnEligibilityService eligibilityService,
            IStoreService storeService = null,
            ILocalizableSettingService localizableSettingService = null)
            : base(null, null, eligibilityService, null, storeService, null, localizableSettingService)
        {
        }

        public Task ValidateRequest(string storeId, IList<CreateReturnItemRequest> items) =>
            ValidateRequestAsync(storeId, null, null, items);

        public void ValidateLines(IList<CreateReturnItemRequest> items) => ValidateNoDuplicateLines(items);

        public Task Validate(Return orderReturn, CustomerOrder order) => ValidateAvailabilityAsync(orderReturn, order);
    }
}
