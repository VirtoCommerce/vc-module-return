using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;
using VirtoCommerce.ReturnModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnQuantityServiceTests
{
    private const string OrderId = "order-1";
    private const string LineId = "line-1";

    [Fact]
    public async Task GetHeldQuantities_Draft_HoldsNothing()
    {
        var service = CreateService(MakeReturn("r1", ReturnStatus.Draft, quantity: 240));

        var held = await service.GetHeldQuantities(OrderId);

        Assert.False(held.ContainsKey(LineId));
    }

    [Fact]
    public async Task GetHeldQuantities_Requested_HoldsWhatWasRequested()
    {
        var service = CreateService(MakeReturn("r1", ReturnStatus.Requested, quantity: 240));

        var held = await service.GetHeldQuantities(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    [Theory]
    [InlineData(ReturnStatus.Cancelled)]
    [InlineData("Canceled")]
    [InlineData(ReturnStatus.Rejected)]
    public async Task GetHeldQuantities_ClosedWithoutAnApproval_HoldsNothing(string status)
    {
        var service = CreateService(MakeReturn("r1", status, quantity: 240));

        var held = await service.GetHeldQuantities(OrderId);

        Assert.False(held.ContainsKey(LineId));
    }

    [Theory]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.PartiallyApproved)]
    public async Task GetHeldQuantities_LineRuledOn_HoldsWhatWasApprovedNotWhatWasAsked(string status)
    {
        var orderReturn = MakeReturn("r1", status, quantity: 240, approvedQuantity: 200);
        orderReturn.LineItems.First().ItemState = ReturnItemState.Approved;

        var service = CreateService(orderReturn);

        var held = await service.GetHeldQuantities(OrderId);

        Assert.Equal(200, held[LineId]);
    }

    [Theory]
    [InlineData(ReturnStatus.AwaitingDelivery)]
    [InlineData(ReturnStatus.Received)]
    [InlineData(ReturnStatus.Processing)]
    [InlineData(ReturnStatus.Completed)]
    public async Task GetHeldQuantities_DecidedReturnCarriedOn_StillHoldsOnlyWhatWasApproved(string status)
    {
        // 200 of 240 bolts approved and the tea declined, then the return is carried on towards a
        // refund: the 40 bolts and the 12 tea turned down stay free to be requested again.
        var orderReturn = MakeReturn("r1", status, quantity: 240, approvedQuantity: 200);
        orderReturn.LineItems.First().ItemState = ReturnItemState.Approved;
        orderReturn.LineItems.Add(new ReturnLineItem { OrderLineItemId = "line-2", Quantity = 12, ItemState = ReturnItemState.Rejected });

        var service = CreateService(orderReturn);

        var held = await service.GetHeldQuantities(OrderId);

        Assert.Equal(200, held[LineId]);
        Assert.False(held.ContainsKey("line-2"));
    }

    [Fact]
    public async Task GetHeldQuantities_ApprovedButNoLineWasRuledOn_StillHoldsWhatWasAsked()
    {
        var service = CreateService(MakeReturn("r1", ReturnStatus.Approved, quantity: 240));

        var held = await service.GetHeldQuantities(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    [Fact]
    public async Task GetHeldQuantities_RejectedLine_HoldsNothing()
    {
        var orderReturn = MakeReturn("r1", ReturnStatus.PartiallyApproved, quantity: 240);
        orderReturn.LineItems.First().ItemState = ReturnItemState.Rejected;

        var service = CreateService(orderReturn);

        var held = await service.GetHeldQuantities(OrderId);

        Assert.False(held.ContainsKey(LineId));
    }

    [Fact]
    public async Task GetHeldQuantities_StatusOutsideTheModel_StillHolds()
    {
        var service = CreateService(MakeReturn("r1", "Processing", quantity: 240));

        var held = await service.GetHeldQuantities(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    [Fact]
    public async Task GetHeldQuantities_SeveralReturns_SumsPerOrderLine()
    {
        var service = CreateService(
            MakeReturn("r1", ReturnStatus.Requested, quantity: 200),
            MakeReturn("r2", ReturnStatus.Requested, quantity: 40),
            MakeReturn("r3", ReturnStatus.Cancelled, quantity: 100));

        var held = await service.GetHeldQuantities(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    [Fact]
    public async Task GetHeldQuantities_ExcludedReturn_IsNotCounted()
    {
        var service = CreateService(
            MakeReturn("r1", ReturnStatus.Requested, quantity: 200),
            MakeReturn("r2", ReturnStatus.Requested, quantity: 40));

        var held = await service.GetHeldQuantities(OrderId, excludeReturnId: "r2");

        Assert.Equal(200, held[LineId]);
    }

    [Fact]
    public async Task GetHeldQuantities_NoOrder_ReturnsEmptyWithoutTouchingTheDatabase()
    {
        var returnService = new Mock<IReturnService>(MockBehavior.Strict);
        var repository = new Mock<IReturnRepository>(MockBehavior.Strict);
        var service = new ReturnQuantityService(() => repository.Object, returnService.Object);

        var held = await service.GetHeldQuantities(null);

        Assert.Empty(held);
    }

    [Fact]
    public async Task GetHeldQuantities_LineWithoutAnOrderLine_IsIgnored()
    {
        var orderReturn = MakeReturn("r1", ReturnStatus.Requested, quantity: 240);
        orderReturn.LineItems.First().OrderLineItemId = null;

        var service = CreateService(orderReturn);

        var held = await service.GetHeldQuantities(OrderId);

        Assert.Empty(held);
    }

    private static ReturnQuantityService CreateService(params Return[] returns)
    {
        var entities = returns
            .Select(x => new ReturnEntity { Id = x.Id, OrderId = OrderId })
            .ToList()
            .BuildMock();

        var repository = new Mock<IReturnRepository>();
        repository.Setup(x => x.Returns).Returns(entities);

        var returnService = new Mock<IReturnService>();
        returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
                returns.Where(x => ids.Contains(x.Id)).ToList());

        return new ReturnQuantityService(() => repository.Object, returnService.Object);
    }

    private static Return MakeReturn(string id, string status, int quantity, int approvedQuantity = 0)
    {
        return new Return
        {
            Id = id,
            OrderId = OrderId,
            Status = status,
            LineItems =
            [
                new ReturnLineItem
                {
                    OrderLineItemId = LineId,
                    Quantity = quantity,
                    ApprovedQuantity = approvedQuantity,
                },
            ],
        };
    }
}
