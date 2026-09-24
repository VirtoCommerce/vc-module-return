using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.ReturnModule.Data.Validation;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnAuthorizationTests
{
    private const string ReturnId = "return-1";

    private readonly Mock<IReturnService> _returnService = new();
    private readonly List<Return> _saved = [];
    private readonly ReturnFlowService _service;

    private Return _orderReturn = NewReturn(ReturnStatus.Requested);

    public ReturnAuthorizationTests()
    {
        _returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => _orderReturn == null ? [] : [_orderReturn]);

        _returnService
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<Return>>()))
            .Callback<IList<Return>>(_saved.AddRange)
            .Returns(Task.CompletedTask);

        _service = new ReturnFlowService(null, _returnService.Object, null, null, new ReturnStateProvider(), null, new ReturnRequestValidator());
    }

    [Fact]
    public async Task EveryLineInFull_IsApproved()
    {
        var result = await Authorize(("line-1", 5, "ignored"), ("line-2", 2, null));

        Assert.Equal(ReturnStatus.Approved, result.Status);
        Assert.All(result.LineItems, x => Assert.Equal(ReturnItemState.Approved, x.ItemState));
        Assert.All(result.LineItems, x => Assert.Null(x.RejectReason));
        Assert.Null(result.RejectReason);
        Assert.Same(result, Assert.Single(_saved));
    }

    [Fact]
    public async Task NothingApproved_IsRejected()
    {
        var result = await Authorize(("line-1", 0, "Used"), ("line-2", 0, "Opened"));

        Assert.Equal(ReturnStatus.Rejected, result.Status);
        Assert.All(result.LineItems, x => Assert.Equal(ReturnItemState.Rejected, x.ItemState));
        Assert.Equal(["Used", "Opened"], result.LineItems.Select(x => x.RejectReason));
        Assert.Equal("Not returnable", result.RejectReason);
    }

    [Fact]
    public async Task SomeApproved_IsPartiallyApproved_AndEachLineIsDecided()
    {
        var result = await Authorize(("line-1", 1, "Four were used"), ("line-2", 0, "Opened"));

        Assert.Equal(ReturnStatus.PartiallyApproved, result.Status);

        var partlyApproved = result.LineItems.Single(x => x.Id == "line-1");
        Assert.Equal(1, partlyApproved.ApprovedQuantity);
        // Decided, so the quantity service holds 1 and releases the other 4 for a new request.
        Assert.Equal(ReturnItemState.Approved, partlyApproved.ItemState);
        Assert.Equal("Four were used", partlyApproved.RejectReason);

        Assert.Equal(ReturnItemState.Rejected, result.LineItems.Single(x => x.Id == "line-2").ItemState);
    }

    [Theory]
    [InlineData(ReturnStatus.Draft)]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.Cancelled)]
    [InlineData("New")]
    public async Task ReturnNotWaitingForADecision_IsRefused(string status)
    {
        _orderReturn = NewReturn(status);

        await AssertRefused(ReturnFlowError.WrongStatus, ("line-1", 5, null), ("line-2", 2, null));
    }

    [Fact]
    public async Task UnknownReturn_IsNotFound()
    {
        _orderReturn = null;

        await AssertRefused(ReturnFlowError.ReturnNotFound, ("line-1", 5, null));
    }

    [Fact]
    public async Task LineLeftUndecided_IsRefused()
    {
        await AssertRefused(ReturnFlowError.InvalidRequest, ("line-1", 5, null));
    }

    [Fact]
    public async Task LineNotOnTheReturn_IsRefused()
    {
        await AssertRefused(ReturnFlowError.LineItemNotFound, ("line-1", 5, null), ("line-2", 2, null), ("line-3", 1, null));
    }

    [Fact]
    public async Task LineDecidedTwice_IsRefused()
    {
        await AssertRefused(ReturnFlowError.DuplicateLine, ("line-1", 5, null), ("LINE-1", 0, null), ("line-2", 2, null));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task ApprovedQuantityOutsideTheRequest_IsRefused(int approvedQuantity)
    {
        await AssertRefused(ReturnFlowError.InvalidQuantity, ("line-1", approvedQuantity, null), ("line-2", 2, null));
    }

    [Fact]
    public async Task OverlongLineReason_IsRefused()
    {
        await AssertRefused(ReturnFlowError.InvalidRequest, ("line-1", 0, new string('x', 1025)), ("line-2", 2, null));
    }

    private async Task AssertRefused(string code, params (string LineItemId, int ApprovedQuantity, string RejectReason)[] decisions)
    {
        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => Authorize(decisions));

        Assert.Equal(code, exception.Code);
        Assert.Empty(_saved);
    }

    private Task<Return> Authorize(params (string LineItemId, int ApprovedQuantity, string RejectReason)[] decisions)
    {
        return _service.Authorize(new ReturnAuthorizationRequest
        {
            ReturnId = ReturnId,
            RejectReason = "Not returnable",
            Items = decisions
                .Select(x => new ReturnLineDecision { LineItemId = x.LineItemId, ApprovedQuantity = x.ApprovedQuantity, RejectReason = x.RejectReason })
                .ToList(),
        }, TestContext.Current.CancellationToken);
    }

    private static Return NewReturn(string status)
    {
        return new Return
        {
            Id = ReturnId,
            Number = "RET260924-00001",
            Status = status,
            LineItems =
            [
                new ReturnLineItem { Id = "line-1", OrderLineItemId = "order-line-1", Quantity = 5, ItemState = ReturnItemState.Requested },
                new ReturnLineItem { Id = "line-2", OrderLineItemId = "order-line-2", Quantity = 2, ItemState = ReturnItemState.Requested },
            ],
        };
    }
}
