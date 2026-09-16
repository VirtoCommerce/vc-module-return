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

/// <summary>
/// How much of an order line existing returns are holding. This is the arithmetic that decides
/// whether a buyer is allowed to return anything at all, so each rule gets its own test.
/// </summary>
public class ReturnQuantityServiceTests
{
    private const string OrderId = "order-1";
    private const string LineId = "line-1";

    /// <summary>
    /// An abandoned draft must not block a line forever: availability is settled when the return is
    /// submitted, not when it is started.
    /// </summary>
    [Fact]
    public async Task GetHeldQuantities_Draft_HoldsNothing()
    {
        var service = CreateService(MakeReturn("r1", ReturnStatus.Draft, quantity: 240));

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.False(held.ContainsKey(LineId));
    }

    [Fact]
    public async Task GetHeldQuantities_Requested_HoldsWhatWasRequested()
    {
        var service = CreateService(MakeReturn("r1", ReturnStatus.Requested, quantity: 240));

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    [Theory]
    [InlineData(ReturnStatus.Cancelled)]
    [InlineData("Canceled")]
    [InlineData(ReturnStatus.Rejected)]
    public async Task GetHeldQuantities_ClosedWithoutAnApproval_HoldsNothing(string status)
    {
        var service = CreateService(MakeReturn("r1", status, quantity: 240));

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.False(held.ContainsKey(LineId));
    }

    /// <summary>
    /// Ask for 240, get 200, and the other 40 are free again — the buyer may legitimately ask for
    /// them later.
    /// </summary>
    [Theory]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.PartiallyApproved)]
    public async Task GetHeldQuantities_LineRuledOn_HoldsWhatWasApprovedNotWhatWasAsked(string status)
    {
        var orderReturn = MakeReturn("r1", status, quantity: 240, approvedQuantity: 200);
        orderReturn.LineItems.First().ItemState = ReturnItemState.Approved;

        var service = CreateService(orderReturn);

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.Equal(200, held[LineId]);
    }

    /// <summary>
    /// "Approved" is also a value in the legacy Return.Status dictionary, and nothing writes
    /// ApprovedQuantity until the agent side lands. Reading it off such a return would report zero
    /// held and hand the buyer units an agent has already promised to somebody.
    /// </summary>
    [Fact]
    public async Task GetHeldQuantities_ApprovedButNoLineWasRuledOn_StillHoldsWhatWasAsked()
    {
        var service = CreateService(MakeReturn("r1", ReturnStatus.Approved, quantity: 240));

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    /// <summary>
    /// A line an agent refused holds nothing, even on a return that is approved as a whole.
    /// </summary>
    [Fact]
    public async Task GetHeldQuantities_RejectedLine_HoldsNothing()
    {
        var orderReturn = MakeReturn("r1", ReturnStatus.PartiallyApproved, quantity: 240);
        orderReturn.LineItems.First().ItemState = ReturnItemState.Rejected;

        var service = CreateService(orderReturn);

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.False(held.ContainsKey(LineId));
    }

    /// <summary>
    /// Returns raised in the admin UI carry values from the editable Return.Status dictionary, which
    /// this module does not control. Counting them is the safe reading: under-counting would let a
    /// buyer return more than they have.
    /// </summary>
    [Fact]
    public async Task GetHeldQuantities_StatusOutsideTheModel_StillHolds()
    {
        var service = CreateService(MakeReturn("r1", "Processing", quantity: 240));

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    [Fact]
    public async Task GetHeldQuantities_SeveralReturns_SumsPerOrderLine()
    {
        var service = CreateService(
            MakeReturn("r1", ReturnStatus.Requested, quantity: 200),
            MakeReturn("r2", ReturnStatus.Requested, quantity: 40),
            MakeReturn("r3", ReturnStatus.Cancelled, quantity: 100));

        var held = await service.GetHeldQuantitiesAsync(OrderId);

        Assert.Equal(240, held[LineId]);
    }

    /// <summary>
    /// A draft competing with itself would read as unavailable the moment it was saved.
    /// </summary>
    [Fact]
    public async Task GetHeldQuantities_ExcludedReturn_IsNotCounted()
    {
        var service = CreateService(
            MakeReturn("r1", ReturnStatus.Requested, quantity: 200),
            MakeReturn("r2", ReturnStatus.Requested, quantity: 40));

        var held = await service.GetHeldQuantitiesAsync(OrderId, excludeReturnId: "r2");

        Assert.Equal(200, held[LineId]);
    }

    [Fact]
    public async Task GetHeldQuantities_NoOrder_ReturnsEmptyWithoutTouchingTheDatabase()
    {
        var returnService = new Mock<IReturnService>(MockBehavior.Strict);
        var repository = new Mock<IReturnRepository>(MockBehavior.Strict);
        var service = new ReturnQuantityService(() => repository.Object, returnService.Object);

        var held = await service.GetHeldQuantitiesAsync(null);

        Assert.Empty(held);
    }

    [Fact]
    public async Task GetHeldQuantities_LineWithoutAnOrderLine_IsIgnored()
    {
        var orderReturn = MakeReturn("r1", ReturnStatus.Requested, quantity: 240);
        orderReturn.LineItems.First().OrderLineItemId = null;

        var service = CreateService(orderReturn);

        var held = await service.GetHeldQuantitiesAsync(OrderId);

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
