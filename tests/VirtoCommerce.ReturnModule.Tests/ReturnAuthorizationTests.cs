using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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

public class ReturnAuthorizationTests
{
    private const string ReturnId = "return-1";

    private readonly Mock<IReturnService> _returnService = new();
    private readonly Mock<ICustomerOrderService> _orderService = new();
    private readonly Mock<IReturnQuantityService> _quantityService = new();
    private readonly Dictionary<string, int> _held = new();
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

        // GetNoCloneAsync is an extension over the CRUD contract, so the mock answers what it calls.
        _orderService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new CustomerOrder { Id = "order-1", Items = [new LineItem { Id = "order-line-1", Quantity = 5 }, new LineItem { Id = "order-line-2", Quantity = 2 }] }]);

        _quantityService
            .Setup(x => x.GetHeldQuantities(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => _held);

        _service = new ReturnFlowService(_orderService.Object, _returnService.Object, null, null, new ReturnStateProvider(), null, new ReturnRequestValidator(), _quantityService.Object);
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

    [Fact]
    public async Task EveryLineApprovedInPart_IsPartiallyApprovedNotApproved()
    {
        var result = await Authorize(("line-1", 1, null), ("line-2", 1, null));

        Assert.Equal(ReturnStatus.PartiallyApproved, result.Status);
    }

    [Fact]
    public async Task ReturnRaisedInTheAdmin_CanBeDecidedToo()
    {
        _orderReturn = NewReturn(ReturnStatus.New);

        var result = await Authorize(("line-1", 0, "Used"), ("line-2", 2, null));

        Assert.Equal(ReturnStatus.PartiallyApproved, result.Status);
    }

    [Fact]
    public async Task ReturnWithoutLines_IsRefused()
    {
        _orderReturn.LineItems.Clear();

        await AssertRefused(ReturnFlowError.NoItems);
    }

    [Fact]
    public async Task EmptyDecision_IsRefused()
    {
        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => _service.Authorize(
            new ReturnAuthorizationRequest { ReturnId = ReturnId, Items = [null] },
            TestContext.Current.CancellationToken));

        Assert.Equal(ReturnFlowError.InvalidRequest, exception.Code);
    }

    [Fact]
    public async Task OverlongReturnReason_IsRefused()
    {
        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => _service.Authorize(
            new ReturnAuthorizationRequest
            {
                ReturnId = ReturnId,
                RejectReason = new string('x', 2049),
                Items =
                [
                    new ReturnLineDecision { LineItemId = "line-1", ApprovedQuantity = 0 },
                    new ReturnLineDecision { LineItemId = "line-2", ApprovedQuantity = 0 },
                ],
            },
            TestContext.Current.CancellationToken));

        Assert.Equal(ReturnFlowError.InvalidRequest, exception.Code);
        Assert.Empty(_saved);
    }

    [Theory]
    [InlineData(ReturnStatus.Draft)]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.Cancelled)]
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

    [Fact]
    public async Task ApprovedPastWhatTheOrderLineHasLeft_IsRefused()
    {
        // Another return holds one of the five, so four is the most this one can be approved for.
        _orderReturn.OrderId = "order-1";
        _held["order-line-1"] = 1;

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => Authorize(("line-1", 5, null), ("line-2", 2, null)));

        Assert.Equal(ReturnFlowError.QuantityUnavailable, exception.Code);
        Assert.Equal(4, Assert.IsType<int>(exception.Values[ReturnFlowErrorValue.AvailableQuantity]));
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task ApprovedWithinWhatTheOrderLineHasLeft_IsSaved()
    {
        _orderReturn.OrderId = "order-1";
        _held["order-line-1"] = 1;

        var result = await Authorize(("line-1", 4, "One was used"), ("line-2", 2, null));

        Assert.Equal(ReturnStatus.PartiallyApproved, result.Status);
        // What the order's other returns hold: this return's own request is what is being decided.
        _quantityService.Verify(x => x.GetHeldQuantities("order-1", ReturnId));
    }

    [Fact]
    public async Task SecondLineForTheSameOrderLine_CountsAgainstTheSameUnits()
    {
        // Written through PUT before it refused a second line: 5 + 1 of the 5 ordered, all of it approved.
        _orderReturn.OrderId = "order-1";
        _orderReturn.LineItems.Add(new ReturnLineItem { Id = "line-3", OrderLineItemId = "order-line-1", Quantity = 1, ItemState = ReturnItemState.Requested });

        await AssertRefused(ReturnFlowError.QuantityUnavailable, ("line-1", 5, null), ("line-2", 2, null), ("line-3", 1, null));
    }

    [Fact]
    public async Task DecliningEverything_IsNotMeasured()
    {
        // Declining only releases units, so it goes through even when the order line has none left.
        _orderReturn.OrderId = "order-1";
        _held["order-line-1"] = 5;
        _held["order-line-2"] = 2;

        var result = await Authorize(("line-1", 0, "Used"), ("line-2", 0, "Opened"));

        Assert.Equal(ReturnStatus.Rejected, result.Status);
        _quantityService.Verify(x => x.GetHeldQuantities(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ServiceBuiltThroughTheOldConstructor_StillAuthorizes()
    {
        // A subclass compiled against 3.1002.0 still reaches it: the check is skipped, nothing fails.
#pragma warning disable VC0016
        var service = new ReturnFlowService(null, _returnService.Object, null, null, new ReturnStateProvider(), null, new ReturnRequestValidator());
#pragma warning restore VC0016
        _orderReturn.OrderId = "order-1";
        _held["order-line-1"] = 5;

        var result = await service.Authorize(NewRequest(("line-1", 5, null), ("line-2", 2, null)), TestContext.Current.CancellationToken);

        Assert.Equal(ReturnStatus.Approved, result.Status);
    }

    [Fact]
    public async Task Container_PicksTheConstructorThatBringsTheQuantityCheck()
    {
        // Registered as Module.Initialize does. With two public constructors the container must neither
        // find them ambiguous nor settle for the old one, which would skip the check.
        var services = new ServiceCollection();
        services.AddTransient<IReturnFlowService, ReturnFlowService>();
        services.AddSingleton(_orderService.Object);
        services.AddSingleton(_returnService.Object);
        services.AddSingleton(Mock.Of<IReturnEligibilityService>());
        services.AddSingleton(Mock.Of<IReturnAttachmentService>());
        services.AddSingleton<IReturnStateProvider>(new ReturnStateProvider());
        services.AddSingleton(Mock.Of<IReturnSettingsService>());
        services.AddSingleton<AbstractValidator<ReturnRequestValidationContext>>(new ReturnRequestValidator());
        services.AddSingleton(_quantityService.Object);
        var service = services.BuildServiceProvider().GetRequiredService<IReturnFlowService>();

        _orderReturn.OrderId = "order-1";
        _held["order-line-1"] = 1;

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() =>
            service.Authorize(NewRequest(("line-1", 5, null), ("line-2", 2, null)), TestContext.Current.CancellationToken));

        Assert.Equal(ReturnFlowError.QuantityUnavailable, exception.Code);
    }

    private async Task AssertRefused(string code, params (string LineItemId, int ApprovedQuantity, string RejectReason)[] decisions)
    {
        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => Authorize(decisions));

        Assert.Equal(code, exception.Code);
        Assert.Empty(_saved);
    }

    private Task<Return> Authorize(params (string LineItemId, int ApprovedQuantity, string RejectReason)[] decisions)
    {
        return _service.Authorize(NewRequest(decisions), TestContext.Current.CancellationToken);
    }

    private static ReturnAuthorizationRequest NewRequest(params (string LineItemId, int ApprovedQuantity, string RejectReason)[] decisions)
    {
        return new ReturnAuthorizationRequest
        {
            ReturnId = ReturnId,
            RejectReason = "Not returnable",
            Items = decisions
                .Select(x => new ReturnLineDecision { LineItemId = x.LineItemId, ApprovedQuantity = x.ApprovedQuantity, RejectReason = x.RejectReason })
                .ToList(),
        };
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
