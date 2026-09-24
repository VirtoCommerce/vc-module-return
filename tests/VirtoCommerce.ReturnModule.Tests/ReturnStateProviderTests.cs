using System.Linq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnStateProviderTests
{
    private readonly ReturnStateProvider _provider = new();

    [Theory]
    [InlineData(ReturnAction.Edit, ReturnStatus.Draft)]
    [InlineData(ReturnAction.Submit, ReturnStatus.Draft)]
    [InlineData(ReturnAction.Cancel, ReturnStatus.Draft)]
    [InlineData(ReturnAction.Cancel, ReturnStatus.Requested)]
    [InlineData(ReturnAction.Authorize, ReturnStatus.Requested)]
    [InlineData(ReturnAction.Authorize, ReturnStatus.New)]
    public void IsAllowed_TransitionInTable_ReturnsTrue(string action, string status)
    {
        Assert.True(_provider.IsAllowed(action, status));
    }

    [Theory]
    [InlineData(ReturnAction.Edit, ReturnStatus.Requested)]
    [InlineData(ReturnAction.Submit, ReturnStatus.Requested)]
    [InlineData(ReturnAction.Cancel, ReturnStatus.Cancelled)]
    [InlineData(ReturnAction.Cancel, ReturnStatus.Approved)]
    [InlineData(ReturnAction.Cancel, ReturnStatus.Rejected)]
    [InlineData(ReturnAction.Authorize, ReturnStatus.Draft)]
    [InlineData(ReturnAction.Authorize, ReturnStatus.Approved)]
    public void IsAllowed_TransitionNotInTable_ReturnsFalse(string action, string status)
    {
        Assert.False(_provider.IsAllowed(action, status));
    }

    [Fact]
    public void IsAllowed_UnknownActionOrStatus_ReturnsFalse()
    {
        Assert.False(_provider.IsAllowed("approve", ReturnStatus.Requested));
        Assert.False(_provider.IsAllowed(ReturnAction.Cancel, "SomethingElse"));
    }

    [Fact]
    public void IsAllowed_NullStatus_ReturnsFalseRatherThanThrowing()
    {
        Assert.False(_provider.IsAllowed(ReturnAction.Cancel, null));
    }

    [Fact]
    public void IsAllowed_IgnoresCase()
    {
        Assert.True(_provider.IsAllowed(ReturnAction.Cancel, "requested"));
        Assert.True(_provider.IsAllowed("CANCEL", ReturnStatus.Requested));
    }

    [Theory]
    [InlineData(ReturnAction.Submit, ReturnStatus.Draft, ReturnStatus.Requested)]
    [InlineData(ReturnAction.Cancel, ReturnStatus.Draft, ReturnStatus.Cancelled)]
    [InlineData(ReturnAction.Cancel, ReturnStatus.Requested, ReturnStatus.Cancelled)]
    public void GetNextStatus_ReturnsTargetOfTransition(string action, string status, string expected)
    {
        Assert.Equal(expected, _provider.GetNextStatus(action, status));
    }

    [Fact]
    public void GetNextStatus_ActionThatDoesNotMoveTheReturn_ReturnsNull()
    {
        Assert.Null(_provider.GetNextStatus(ReturnAction.Edit, ReturnStatus.Draft));
    }

    [Fact]
    public void GetNextStatus_TransitionNotInTable_ReturnsNull()
    {
        Assert.Null(_provider.GetNextStatus(ReturnAction.Submit, ReturnStatus.Requested));
    }

    [Fact]
    public void GetActions_ReportsEveryKnownAction_AvailableOrNot()
    {
        var orderReturn = new Return { Status = ReturnStatus.Requested };

        var actions = _provider.GetActions(orderReturn);

        Assert.Equal(
            new[] { ReturnAction.Edit, ReturnAction.Submit, ReturnAction.Cancel },
            actions.Select(x => x.Name));
    }

    [Fact]
    public void GetActions_NeverOffersTheAgentsDecisionToTheBuyer()
    {
        var actions = _provider.GetActions(new Return { Status = ReturnStatus.Requested });

        Assert.DoesNotContain(actions, x => x.Name == ReturnAction.Authorize);
    }

    [Theory]
    // Into a status only the flow sets.
    [InlineData(ReturnStatus.New, ReturnStatus.Draft)]
    [InlineData(ReturnStatus.New, ReturnStatus.Requested)]
    [InlineData(ReturnStatus.New, ReturnStatus.Approved)]
    [InlineData(ReturnStatus.New, ReturnStatus.PartiallyApproved)]
    [InlineData(ReturnStatus.New, ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Processing, "approved")]
    // Out of a status the buyer owns, or a closed one.
    [InlineData(ReturnStatus.Draft, ReturnStatus.Processing)]
    [InlineData(ReturnStatus.Requested, ReturnStatus.Cancelled)]
    [InlineData(ReturnStatus.Rejected, ReturnStatus.Completed)]
    [InlineData(ReturnStatus.Cancelled, ReturnStatus.New)]
    [InlineData("Canceled", ReturnStatus.Processing)]
    [InlineData("cancelled", ReturnStatus.Processing)]
    public void CanSetStatus_FlowStatus_IsRefused(string oldStatus, string newStatus)
    {
        Assert.False(_provider.CanSetStatus(Undecided(oldStatus), newStatus));
    }

    [Theory]
    [InlineData(ReturnStatus.New, ReturnStatus.Processing)]
    [InlineData(ReturnStatus.New, ReturnStatus.Cancelled)]
    [InlineData(ReturnStatus.New, "Canceled")]
    [InlineData(ReturnStatus.Processing, ReturnStatus.Completed)]
    [InlineData("SomethingCustom", ReturnStatus.New)]
    public void CanSetStatus_UndecidedReturn_MovesFreelyOutsideTheFlow(string oldStatus, string newStatus)
    {
        Assert.True(_provider.CanSetStatus(Undecided(oldStatus), newStatus));
    }

    [Theory]
    [InlineData(ReturnStatus.Approved, ReturnStatus.Completed)]
    [InlineData(ReturnStatus.PartiallyApproved, ReturnStatus.Processing)]
    [InlineData(ReturnStatus.PartiallyApproved, ReturnStatus.AwaitingDelivery)]
    [InlineData(ReturnStatus.Processing, ReturnStatus.Received)]
    public void CanSetStatus_DecidedReturn_CanBeCarriedOut(string oldStatus, string newStatus)
    {
        Assert.True(_provider.CanSetStatus(Decided(oldStatus), newStatus));
    }

    [Theory]
    [InlineData(ReturnStatus.Approved, ReturnStatus.New)]
    [InlineData(ReturnStatus.Approved, ReturnStatus.Cancelled)]
    [InlineData(ReturnStatus.Processing, ReturnStatus.Cancelled)]
    [InlineData(ReturnStatus.Completed, "SomethingCustom")]
    public void CanSetStatus_DecidedReturn_CannotBeUndone(string oldStatus, string newStatus)
    {
        Assert.False(_provider.CanSetStatus(Decided(oldStatus), newStatus));
    }

    [Theory]
    [InlineData(ReturnStatus.Requested, ReturnStatus.Requested)]
    [InlineData(ReturnStatus.Approved, "approved")]
    [InlineData("Canceled", ReturnStatus.Cancelled)]
    public void CanSetStatus_SameStatus_IsAlwaysFine(string oldStatus, string newStatus)
    {
        Assert.True(_provider.CanSetStatus(Decided(oldStatus), newStatus));
    }

    [Theory]
    [InlineData(ReturnStatus.New, true)]
    [InlineData(ReturnStatus.Approved, false)]
    [InlineData(ReturnStatus.Requested, false)]
    public void CanSetStatus_NewReturn_StartsOutsideTheFlow(string status, bool expected)
    {
        Assert.Equal(expected, _provider.CanSetStatus(null, status));
    }

    private static Return Undecided(string status)
    {
        return new Return { Status = status, LineItems = [new ReturnLineItem { ItemState = ReturnItemState.Requested }] };
    }

    private static Return Decided(string status)
    {
        return new Return { Status = status, LineItems = [new ReturnLineItem { ItemState = ReturnItemState.Approved }] };
    }

    [Fact]
    public void GetActions_UnavailableAction_CarriesTheCodeTheMutationWouldFailWith()
    {
        var orderReturn = new Return { Status = ReturnStatus.Requested };

        var actions = _provider.GetActions(orderReturn);

        var edit = actions.Single(x => x.Name == ReturnAction.Edit);
        Assert.False(edit.IsAvailable);
        Assert.Equal(ReturnFlowError.WrongStatus, edit.UnavailableReason);

        var cancel = actions.Single(x => x.Name == ReturnAction.Cancel);
        Assert.True(cancel.IsAvailable);
        Assert.Null(cancel.UnavailableReason);
    }

    [Fact]
    public void GetActions_ClosedReturn_OffersNothing()
    {
        var orderReturn = new Return { Status = ReturnStatus.Cancelled };

        var actions = _provider.GetActions(orderReturn);

        Assert.All(actions, action => Assert.False(action.IsAvailable));
    }
}
