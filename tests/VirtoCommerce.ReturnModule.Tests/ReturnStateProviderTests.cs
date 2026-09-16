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
