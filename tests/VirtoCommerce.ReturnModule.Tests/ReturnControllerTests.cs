using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.ReturnModule.Web.Controllers.Api;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnControllerTests
{
    private const string ReturnId = "return-1";
    private const string LineItemId = "line-1";
    private const string OrderLineItemId = "order-line-1";

    private readonly Mock<IReturnService> _returnService = new();
    private readonly Mock<IReturnFlowService> _returnFlowService = new();
    private readonly Mock<IReturnQuantityService> _quantityService = new();
    private readonly Mock<ICustomerOrderService> _orderService = new();
    private readonly Dictionary<string, int> _held = new();
    private readonly List<Return> _saved = [];
    private readonly ReturnController _controller;

    private Return _storedReturn;

    public ReturnControllerTests()
    {
        _orderService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new CustomerOrder { Id = "order-1", Items = [new LineItem { Id = OrderLineItemId, Quantity = 2 }] }]);

        _quantityService
            .Setup(x => x.GetHeldQuantities(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => _held);

        _returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => _storedReturn == null ? [] : [_storedReturn]);

        _returnService
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<Return>>()))
            .Callback<IList<Return>>(_saved.AddRange)
            .Returns(Task.CompletedTask);

        _controller = new ReturnController(
            Mock.Of<IReturnSearchService>(),
            _returnService.Object,
            _returnFlowService.Object,
            new ReturnStateProvider(),
            _quantityService.Object,
            _orderService.Object);
    }

    [Fact]
    public async Task UpdateReturn_KeepsTheStoredDecisionWhateverIsSent()
    {
        _storedReturn = NewReturn(ReturnStatus.PartiallyApproved, approvedQuantity: 1, itemState: ReturnItemState.Approved);
        _storedReturn.RejectReason = "One was used";
        _storedReturn.LineItems.Single().RejectReason = "Opened";

        var edited = NewReturn(ReturnStatus.PartiallyApproved, approvedQuantity: 999, itemState: ReturnItemState.Requested);
        edited.RejectReason = "forged";
        edited.LineItems.Single().RejectReason = "forged";

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<OkObjectResult>(result);
        var lineItem = Assert.Single(Assert.Single(_saved).LineItems);
        Assert.Equal(1, lineItem.ApprovedQuantity);
        Assert.Equal(ReturnItemState.Approved, lineItem.ItemState);
        Assert.Equal("Opened", lineItem.RejectReason);
        Assert.Equal("One was used", _saved[0].RejectReason);
    }

    [Fact]
    public async Task UpdateReturn_DecidedLine_KeepsTheQuantityItWasDecidedOn()
    {
        _storedReturn = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);

        var edited = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);
        edited.LineItems.Single().Quantity = 1;

        await _controller.UpdateReturn(edited);

        var lineItem = Assert.Single(Assert.Single(_saved).LineItems);
        Assert.Equal(2, lineItem.Quantity);
        Assert.Equal(2, lineItem.ApprovedQuantity);
    }

    [Fact]
    public async Task UpdateReturn_UndecidedLine_QuantityCanStillBeCorrected()
    {
        _storedReturn = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);

        var edited = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);
        edited.LineItems.Single().Quantity = 1;

        await _controller.UpdateReturn(edited);

        Assert.Equal(1, Assert.Single(Assert.Single(_saved).LineItems).Quantity);
    }

    [Fact]
    public async Task UpdateReturn_LineAddedToADecidedReturn_IsRefused()
    {
        _storedReturn = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);

        var edited = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);
        edited.LineItems.Add(new ReturnLineItem { OrderLineItemId = OrderLineItemId, Quantity = 1 });

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_NewLine_StartsUndecided()
    {
        var created = NewReturn("New", approvedQuantity: 2, itemState: ReturnItemState.Approved);
        created.Id = null;

        await _controller.UpdateReturn(created);

        var lineItem = Assert.Single(Assert.Single(_saved).LineItems);
        Assert.Equal(0, lineItem.ApprovedQuantity);
        Assert.Null(lineItem.ItemState);
    }

    [Fact]
    public async Task UpdateReturn_StatusTheStateProviderRefuses_IsNotSaved()
    {
        // The rules themselves are the state provider's, tested there; this is that PUT asks it.
        _storedReturn = NewReturn(ReturnStatus.New);

        var result = await _controller.UpdateReturn(NewReturn(ReturnStatus.Approved));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_StatusTheStateProviderAllows_IsSaved()
    {
        _storedReturn = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);

        var result = await _controller.UpdateReturn(NewReturn(ReturnStatus.Completed, approvedQuantity: 2, itemState: ReturnItemState.Approved));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(ReturnStatus.Completed, Assert.Single(_saved).Status);
    }

    [Fact]
    public async Task UpdateReturn_NoBody_IsABadRequest()
    {
        Assert.IsType<BadRequestResult>(await _controller.UpdateReturn(null));
    }

    [Fact]
    public async Task UpdateReturn_QuantityOtherReturnsHold_IsNotAvailable()
    {
        _storedReturn = NewReturn(ReturnStatus.New);
        _held[OrderLineItemId] = 1;

        var result = await _controller.UpdateReturn(NewReturn(ReturnStatus.New));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateReturn_QuantityAPartialApprovalReleased_IsAvailableAgain()
    {
        // Another return asked for both units and got one approved: the quantity service holds 1.
        _storedReturn = NewReturn(ReturnStatus.New);
        _held[OrderLineItemId] = 1;

        var edited = NewReturn(ReturnStatus.New);
        edited.LineItems.Single().Quantity = 1;

        Assert.IsType<OkObjectResult>(await _controller.UpdateReturn(edited));
        _quantityService.Verify(x => x.GetHeldQuantities("order-1", ReturnId));
    }

    [Fact]
    public async Task AuthorizeReturn_NoBody_IsABadRequest()
    {
        Assert.IsType<BadRequestResult>((await _controller.AuthorizeReturn(ReturnId, null)).Result);
    }

    [Fact]
    public async Task AuthorizeReturn_PassesTheRouteIdToTheFlow()
    {
        _returnFlowService
            .Setup(x => x.Authorize(It.IsAny<ReturnAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewReturn(ReturnStatus.Approved));

        var result = await _controller.AuthorizeReturn(ReturnId, new ReturnAuthorizationRequest { ReturnId = "something-else" });

        Assert.IsType<OkObjectResult>(result.Result);
        _returnFlowService.Verify(x => x.Authorize(It.Is<ReturnAuthorizationRequest>(r => r.ReturnId == ReturnId), It.IsAny<CancellationToken>()));
    }

    [Theory]
    [InlineData(ReturnFlowError.ReturnNotFound, typeof(NotFoundResult))]
    [InlineData(ReturnFlowError.WrongStatus, typeof(BadRequestObjectResult))]
    [InlineData(ReturnFlowError.InvalidQuantity, typeof(BadRequestObjectResult))]
    public async Task AuthorizeReturn_FlowRefusal_IsAClientError(string code, System.Type expected)
    {
        _returnFlowService
            .Setup(x => x.Authorize(It.IsAny<ReturnAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ReturnFlowException(code, "refused"));

        var result = await _controller.AuthorizeReturn(ReturnId, new ReturnAuthorizationRequest());

        Assert.IsType(expected, result.Result);
    }

    private static Return NewReturn(string status, int approvedQuantity = 0, string itemState = null)
    {
        return new Return
        {
            Id = ReturnId,
            OrderId = "order-1",
            Status = status,
            LineItems =
            [
                new ReturnLineItem
                {
                    Id = LineItemId,
                    OrderLineItemId = OrderLineItemId,
                    Quantity = 2,
                    ApprovedQuantity = approvedQuantity,
                    ItemState = itemState,
                },
            ],
        };
    }
}
