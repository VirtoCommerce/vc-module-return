using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.ReturnModule.Data.Validation;
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
    public async Task QuantityUnavailable_CarriesTheNumbersRatherThanOnlyTheSentence()
    {
        // The storefront renders this per line in its own language, so parsing the English message
        // is not an option.
        var service = CreateService(returnableQuantity: 4);

        var orderReturn = new Return
        {
            LineItems = [new ReturnLineItem { OrderLineItemId = LineId, Quantity = 6 }],
        };

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.Validate(orderReturn, new CustomerOrder()));

        Assert.Equal(ReturnFlowError.QuantityUnavailable, exception.Code);
        Assert.Equal(LineId, exception.Values[ReturnFlowErrorValue.OrderLineItemId]);
        Assert.Equal(6, exception.Values[ReturnFlowErrorValue.RequestedQuantity]);
        Assert.Equal(4, exception.Values[ReturnFlowErrorValue.AvailableQuantity]);
    }

    [Fact]
    public async Task LineNotReturnable_CarriesTheReasonRatherThanOnlyTheSentence()
    {
        // The same race reaches the buyer as this code once the units are gone entirely.
        var service = CreateService(returnableQuantity: 0, isReturnable: false);

        var orderReturn = new Return
        {
            LineItems = [new ReturnLineItem { OrderLineItemId = LineId, Quantity = 1 }],
        };

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.Validate(orderReturn, new CustomerOrder()));

        Assert.Equal(ReturnFlowError.LineNotReturnable, exception.Code);
        Assert.Equal(LineId, exception.Values[ReturnFlowErrorValue.OrderLineItemId]);
        Assert.Equal(ReturnIneligibilityReason.NothingLeftToReturn, exception.Values[ReturnFlowErrorValue.IneligibilityReason]);
        Assert.Equal(0, exception.Values[ReturnFlowErrorValue.AvailableQuantity]);
    }

    [Fact]
    public async Task LineNotOnTheOrder_NamesTheLine()
    {
        var service = CreateService(returnableQuantity: 5);

        var orderReturn = new Return
        {
            LineItems = [new ReturnLineItem { OrderLineItemId = "not-on-this-order", Quantity = 1 }],
        };

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.Validate(orderReturn, new CustomerOrder()));

        Assert.Equal(ReturnFlowError.LineItemNotFound, exception.Code);
        Assert.Equal("not-on-this-order", exception.Values[ReturnFlowErrorValue.OrderLineItemId]);
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
    public async Task LineWithoutAReason_PassesWhileDrafting()
    {
        // Autosave writes the draft on every keystroke, so an unfinished line is not an invalid one.
        var service = CreateServiceWithReasons("FaultyOnArrival");

        await service.ValidateRequest(
            "store-1",
            [new CreateReturnItemRequest { OrderLineItemId = LineId, Quantity = 1 }]);
    }

    [Fact]
    public async Task LineWithoutAReason_IsRefusedAtSubmit()
    {
        // Clearing the reason used to slip past both reason rules and take the mandatory-comment
        // setting with it.
        var service = CreateServiceWithReasons("FaultyOnArrival");

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.ValidateRequest(
                "store-1",
                [new CreateReturnItemRequest { OrderLineItemId = LineId, Quantity = 1 }],
                requireReason: true));

        Assert.Equal(ReturnFlowError.InvalidRequest, exception.Code);
    }

    [Fact]
    public async Task HeaderOnlyUpdate_IsValidatedToo()
    {
        // An update that sends no items still writes the reference, so it still has to pass.
        var service = CreateServiceWithReasons("FaultyOnArrival");

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.ValidateHeader("store-1", new string('x', 129)));

        Assert.Equal(ReturnFlowError.InvalidRequest, exception.Code);
    }

    [Fact]
    public async Task HeaderOnlyUpdate_WithinTheLimits_Passes()
    {
        var service = CreateServiceWithReasons("FaultyOnArrival");

        await service.ValidateHeader("store-1", "PO-7788");
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
        var settingsService = new Mock<IReturnSettingsService>();
        settingsService
            .Setup(x => x.GetRulesAsync(It.IsAny<string>()))
            .ReturnsAsync(new ReturnStoreRules { Reasons = reasons });

        return new TestableReturnFlowService(new Mock<IReturnEligibilityService>().Object, settingsService.Object);
    }

    private static TestableReturnFlowService CreateService(int returnableQuantity = 0, bool isReturnable = true)
    {
        var eligibilityService = new Mock<IReturnEligibilityService>();
        eligibilityService
            .Setup(x => x.GetReturnableItems(It.IsAny<CustomerOrder>(), It.IsAny<string>()))
            .ReturnsAsync(() =>
            [
                new ReturnableItem
                {
                    OrderLineItemId = LineId,
                    IsReturnable = isReturnable,
                    IneligibilityReason = isReturnable ? null : ReturnIneligibilityReason.NothingLeftToReturn,
                    ReturnableQuantity = returnableQuantity,
                },
            ]);

        return new TestableReturnFlowService(eligibilityService.Object);
    }

    private sealed class TestableReturnFlowService : ReturnFlowService
    {
        public TestableReturnFlowService(
            IReturnEligibilityService eligibilityService,
            IReturnSettingsService settingsService = null)
            : base(null, null, eligibilityService, null, null, settingsService, new ReturnRequestValidator())
        {
        }

        public Task ValidateRequest(string storeId, IList<CreateReturnItemRequest> items, bool requireReason = false) =>
            ValidateRequestAsync(storeId, null, null, items, requireReason);

        public Task ValidateHeader(string storeId, string customerReference) =>
            ValidateRequestAsync(storeId, customerReference, null, null);

        public void ValidateLines(IList<CreateReturnItemRequest> items) => ValidateNoDuplicateLines(items);

        public Task Validate(Return orderReturn, CustomerOrder order) => ValidateAvailabilityAsync(orderReturn, order);
    }
}
