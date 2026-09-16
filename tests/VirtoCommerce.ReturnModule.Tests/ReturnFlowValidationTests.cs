using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
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
    public async Task Availability_WithinTheLimit_Passes()
    {
        var service = CreateService(returnableQuantity: 5);

        var orderReturn = new Return
        {
            LineItems = [new ReturnLineItem { OrderLineItemId = LineId, Quantity = 5 }],
        };

        await service.Validate(orderReturn, new CustomerOrder());
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
        public TestableReturnFlowService(IReturnEligibilityService eligibilityService)
            : base(null, null, eligibilityService, null, null, null)
        {
        }

        public void ValidateLines(IList<CreateReturnItemRequest> items) => ValidateNoDuplicateLines(items);

        public Task Validate(Return orderReturn, CustomerOrder order) => ValidateAvailabilityAsync(orderReturn, order);
    }
}
