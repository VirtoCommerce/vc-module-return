using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Repositories;
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
    // The real rule for what a line holds; only what the other returns hold is stood in for.
    private readonly Mock<ReturnQuantityService> _quantityService = new((Func<IReturnRepository>)(() => null), Mock.Of<IReturnService>())
    {
        CallBase = true,
    };

    private readonly Mock<ICustomerOrderService> _orderService = new();
    private readonly Mock<ISettingsManager> _settingsManager = new();
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

        _settingsManager
            .Setup(x => x.GetObjectSettingAsync(ModuleConstants.Settings.General.OrderStatus.Name, null, null))
            .ReturnsAsync(new ObjectSettingEntry(ModuleConstants.Settings.General.OrderStatus));

        _controller = new ReturnController(
            Mock.Of<IReturnSearchService>(),
            _returnService.Object,
            _returnFlowService.Object,
            new ReturnStateProvider(),
            _quantityService.Object,
            _orderService.Object,
            _settingsManager.Object);
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
    public async Task UpdateReturn_SubmittedDateSent_IsNotTaken()
    {
        // A date sent with a draft would hand it to the organization; only the save stamps the submit.
        _storedReturn = NewReturn(ReturnStatus.Draft);

        var edited = NewReturn(ReturnStatus.Draft);
        edited.SubmittedDate = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(Assert.Single(_saved).SubmittedDate);
    }

    [Fact]
    public async Task UpdateReturn_DecidedLineQuantityChanged_IsRefused()
    {
        // It used to be put back silently, so a REST caller got a 200 for a change that never happened.
        _storedReturn = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);

        var edited = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);
        edited.LineItems.Single().Quantity = 1;

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
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
        edited.LineItems.Add(new ReturnLineItem { OrderLineItemId = "order-line-2", Quantity = 1 });

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_SecondLineForTheSameOrderLine_IsRefused()
    {
        // 2 of 2 held, plus a line of 1 for the same order line: each line used to be measured against
        // what is left as if the other did not exist, and the return ended up holding 3 of 2.
        _storedReturn = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);

        var edited = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);
        edited.LineItems.Add(new ReturnLineItem { OrderLineItemId = OrderLineItemId, Quantity = 1 });

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_SecondLineForTheSameOrderLine_IsRefusedEvenWhenTheTotalFits()
    {
        // One line per order line, as the storefront asks: the module finds a line by its order line.
        var created = NewReturn(ReturnStatus.New);
        created.Id = null;
        created.LineItems.Single().Id = null;
        created.LineItems.Single().Quantity = 1;
        created.LineItems.Add(new ReturnLineItem { OrderLineItemId = OrderLineItemId, Quantity = 1 });

        var result = await _controller.UpdateReturn(created);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateReturn_WithoutLines_IsABadRequest(bool emptyList)
    {
        // A missing list failed with a 500 on the first check that read it; an empty one saved a return with no lines.
        _storedReturn = NewReturn(ReturnStatus.New);

        var edited = NewReturn(ReturnStatus.New);
        edited.LineItems = emptyList ? [] : null;

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_EmptyLine_IsABadRequest()
    {
        _storedReturn = NewReturn(ReturnStatus.New);

        var edited = NewReturn(ReturnStatus.New);
        edited.LineItems.Add(null);

        Assert.IsType<BadRequestObjectResult>(await _controller.UpdateReturn(edited));
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_LineOfAnotherReturn_IsRefused()
    {
        // The line is saved by its id: one the return does not have is inserted as a new row, under the key
        // the other return's line already has, and the database refused it with a 500.
        _storedReturn = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);

        var edited = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);
        edited.LineItems.Single().Id = "line-of-another-return";

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_NewLineInPlaceOfTheOldOne_IsSaved()
    {
        // The control for the test above: the same exchange with the new line left without an id.
        _storedReturn = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);

        var edited = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);
        edited.LineItems.Single().Id = null;

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(Assert.Single(Assert.Single(_saved).LineItems).Id);
    }

    [Fact]
    public async Task UpdateReturn_NewReturnWhoseLineCarriesAnId_IsRefused()
    {
        // A new return has no lines yet, so a line with an id can only be someone else's.
        var created = NewReturn(ReturnStatus.New);
        created.Id = null;
        created.LineItems.Single().Id = "line-of-another-return";

        var result = await _controller.UpdateReturn(created);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateReturn_NewReturnWithoutStatus_IsRefused(string status)
    {
        // There is no status to keep yet, so an empty one used to pass as kept: the return was saved with none,
        // held its units and could not be approved or declined.
        var created = NewReturn(status);
        created.Id = null;
        created.LineItems.Single().Id = null;

        var result = await _controller.UpdateReturn(created);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_ReturnStoredWithoutStatus_CanBeGivenOne()
    {
        // The control for the test above: only a new return needs a status, so one saved without it can be repaired.
        _storedReturn = NewReturn(null);

        var result = await _controller.UpdateReturn(NewReturn(ReturnStatus.New));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(ReturnStatus.New, Assert.Single(_saved).Status);
    }

    [Fact]
    public async Task UpdateReturn_OldSpellingOfCancelled_IsStillAccepted()
    {
        // Only the status list offers a single spelling; a client that sends the old one keeps working.
        _storedReturn = NewReturn(ReturnStatus.New);

        var result = await _controller.UpdateReturn(NewReturn(ReturnStatus.LegacyCancelled));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateReturn_NewLine_StartsUndecided()
    {
        var created = NewReturn("New", approvedQuantity: 2, itemState: ReturnItemState.Approved);
        created.Id = null;
        created.LineItems.Single().Id = null;

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
    public async Task UpdateReturn_StatusOutsideTheDictionary_IsNotSaved()
    {
        // The state provider alone lets an open return move to anything at all; the status list only
        // ever offers what the dictionary has, and PUT now asks the same question.
        _storedReturn = NewReturn(ReturnStatus.New);

        var result = await _controller.UpdateReturn(NewReturn("Shipped"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_DecidedReturnMovedToAFulfilmentStatusTheDictionaryLacks_IsNotSaved()
    {
        // AwaitingDelivery is where a decided return may go, but it is not in the shipped dictionary.
        _storedReturn = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);

        var result = await _controller.UpdateReturn(NewReturn(ReturnStatus.AwaitingDelivery, approvedQuantity: 2, itemState: ReturnItemState.Approved));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_KeepingAStatusOutsideTheDictionary_IsSaved()
    {
        // A return already in a status the dictionary no longer has can still be edited in other ways.
        _storedReturn = NewReturn("Shipped");

        var result = await _controller.UpdateReturn(NewReturn("Shipped"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(_saved);
    }

    [Fact]
    public async Task UpdateReturn_QuantityMeasuredAgainstTheStoredOrder_NotThePostedOne()
    {
        // The stored order has 2 units; a body claiming the order has 99 must not make room for 5.
        _storedReturn = NewReturn(ReturnStatus.New);
        var orderReturn = NewReturn(ReturnStatus.New);
        orderReturn.LineItems.First().Quantity = 5;
        orderReturn.Order = new CustomerOrder { Id = "order-1", Items = [new LineItem { Id = OrderLineItemId, Quantity = 99 }] };

        var result = await _controller.UpdateReturn(orderReturn);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
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

    [Theory]
    [InlineData(ReturnStatus.PartiallyApproved)]
    [InlineData(ReturnStatus.Completed)]
    public async Task UpdateReturn_DecidedReturnWhoseReleasedUnitsWereRequestedAgain_IsSaved(string status)
    {
        // Both units asked for, one approved, and the released one since requested again by another
        // return. The decided line still asks for 2 but holds only 1 - exactly what is left for it -
        // so neither a resolution-only edit nor carrying the return on may be refused.
        _storedReturn = NewReturn(ReturnStatus.PartiallyApproved, approvedQuantity: 1, itemState: ReturnItemState.Approved);
        _held[OrderLineItemId] = 1;

        var edited = NewReturn(status, approvedQuantity: 1, itemState: ReturnItemState.Approved);
        edited.Resolution = "Refunded one unit";

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(status, Assert.Single(_saved).Status);
    }

    [Theory]
    [InlineData(ReturnStatus.Rejected, ReturnItemState.Rejected)]
    [InlineData(ReturnStatus.Cancelled, ReturnItemState.Requested)]
    public async Task UpdateReturn_ReturnThatHoldsNothing_IsNotMeasuredAgainstWhatOthersHold(string status, string itemState)
    {
        // Declined, or cancelled by a buyer who then raised a fresh return for the same units: it
        // released everything, and another return has since asked for all of it. Its stored quantity
        // no longer competes with anything, so an edit that leaves it alone must still save.
        _storedReturn = NewReturn(status, itemState: itemState);
        _held[OrderLineItemId] = 2;

        var edited = NewReturn(status, itemState: itemState);
        edited.Resolution = "Nothing to refund";

        Assert.IsType<OkObjectResult>(await _controller.UpdateReturn(edited));
    }

    [Theory]
    [InlineData(ReturnStatus.Cancelled)]
    [InlineData("Canceled")]
    public async Task UpdateReturn_ReturnCreatedStraightAsCancelled_CannotAskForMoreThanIsLeft(string status)
    {
        // A return saved straight as cancelled holds nothing, so measuring it by what it holds accepted
        // 99 of 5 ordered units. What a line asks for is measured when it is written, whatever the status.
        var created = NewReturn(status);
        created.Id = null;
        created.LineItems.Single().Id = null;
        created.LineItems.Single().Quantity = 3;

        var result = await _controller.UpdateReturn(created);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Theory]
    [InlineData(ReturnStatus.Cancelled)]
    [InlineData(ReturnStatus.Draft)]
    public async Task UpdateReturn_QuantityRaisedOnAReturnThatHoldsNothing_IsRefused(string status)
    {
        // The same through an edit: raising a line past what the order has left is refused even though
        // the return - cancelled, or still a draft - holds nothing.
        _storedReturn = NewReturn(status, itemState: ReturnItemState.Requested);
        _storedReturn.LineItems.Single().Quantity = 1;

        var edited = NewReturn(status, itemState: ReturnItemState.Requested);
        edited.LineItems.Single().Quantity = 3;

        var result = await _controller.UpdateReturn(edited);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task UpdateReturn_DecidedLine_StillHasToFitWhatItHolds()
    {
        // A decided line is not waved through: its approved units are held, and they have to fit what
        // the other returns leave - here the order line shrank to less than both returns hold.
        _storedReturn = NewReturn(ReturnStatus.Approved, approvedQuantity: 2, itemState: ReturnItemState.Approved);
        _held[OrderLineItemId] = 1;

        var result = await _controller.UpdateReturn(NewReturn(ReturnStatus.Completed, approvedQuantity: 2, itemState: ReturnItemState.Approved));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task GetAvailableStatuses_DecidedReturn_OffersItsOwnStatusAndWhereItGoesNext()
    {
        // What the admin's status list offered before: New, Canceled and the rest, every one refused on save.
        _storedReturn = NewReturn(ReturnStatus.PartiallyApproved, approvedQuantity: 1, itemState: ReturnItemState.Approved);

        var result = await _controller.GetAvailableStatuses(ReturnId);

        var statuses = Assert.IsType<string[]>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal([ReturnStatus.Completed, ReturnStatus.Processing, ReturnStatus.PartiallyApproved], statuses);
    }

    [Fact]
    public async Task GetAvailableStatuses_RequestedReturn_OffersOnlyItsOwnStatus()
    {
        // A request moves on only by being authorized or cancelled, never by an edit.
        _storedReturn = NewReturn(ReturnStatus.Requested, itemState: ReturnItemState.Requested);

        var result = await _controller.GetAvailableStatuses(ReturnId);

        var statuses = Assert.IsType<string[]>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal([ReturnStatus.Requested], statuses);
    }

    [Fact]
    public async Task GetAvailableStatuses_OpenReturn_OffersCancelledOnce()
    {
        // The dictionary has both spellings, which read as the same word in every language but English.
        _storedReturn = NewReturn(ReturnStatus.New);

        var statuses = await GetAvailableStatuses();

        Assert.Equal([ReturnStatus.New, ReturnStatus.Completed, ReturnStatus.Cancelled, ReturnStatus.Processing], statuses);
    }

    [Fact]
    public async Task GetAvailableStatuses_ReturnStoredWithTheOldSpelling_OffersItsOwn()
    {
        // The selector can show the return's status only if it is one of the options.
        _storedReturn = NewReturn(ReturnStatus.LegacyCancelled);

        Assert.Equal([ReturnStatus.LegacyCancelled], await GetAvailableStatuses());
    }

    [Fact]
    public async Task GetAvailableStatuses_DictionaryWithOnlyTheOldSpelling_StillOffersIt()
    {
        // A dictionary edited before Cancelled shipped keeps its own list: the stored one replaces the shipped one.
        _settingsManager
            .Setup(x => x.GetObjectSettingAsync(ModuleConstants.Settings.General.OrderStatus.Name, null, null))
            .ReturnsAsync(new ObjectSettingEntry(ModuleConstants.Settings.General.OrderStatus) { AllowedValues = [ReturnStatus.New, ReturnStatus.LegacyCancelled] });
        _storedReturn = NewReturn(ReturnStatus.New);

        Assert.Equal([ReturnStatus.New, ReturnStatus.LegacyCancelled], await GetAvailableStatuses());
    }

    [Fact]
    public async Task GetAvailableStatuses_MissingReturn_IsNotFound()
    {
        Assert.IsType<NotFoundResult>((await _controller.GetAvailableStatuses("gone")).Result);
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

    private async Task<string[]> GetAvailableStatuses()
    {
        var result = await _controller.GetAvailableStatuses(ReturnId);

        return Assert.IsType<string[]>(Assert.IsType<OkObjectResult>(result.Result).Value);
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
