using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.ReturnModule.Data.Validation;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

/// <summary>
/// A colleague who may read a return through the organization scope still may not change it: every
/// mutation finds the return by its buyer, and the organization never takes part in that lookup.
/// </summary>
public class ReturnOwnershipTests
{
    private const string ReturnId = "return-1";
    private const string BuyerId = "buyer-1";
    private const string ColleagueId = "buyer-2";

    private readonly Mock<IReturnService> _returnService = new();
    private readonly List<Return> _saved = [];
    private readonly ReturnFlowService _service;

    private Return _orderReturn;

    public ReturnOwnershipTests()
    {
        _returnService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [_orderReturn]);

        _returnService
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<Return>>()))
            .Callback<IList<Return>>(_saved.AddRange)
            .Returns(Task.CompletedTask);

        // Ownership is decided before anything else is reached, so the lookup and the transition
        // table are all the flow needs here.
        _service = new ReturnFlowService(null, _returnService.Object, null, null, new ReturnStateProvider(), null, new ReturnRequestValidator());
    }

    [Fact]
    public async Task ColleagueCancelling_IsRefusedAsNotFound()
    {
        _orderReturn = NewReturn(ReturnStatus.Requested);

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => _service.Cancel(ReturnId, "Not needed", Colleague(), TestContext.Current.CancellationToken));

        Assert.Equal(ReturnFlowError.ReturnNotFound, exception.Code);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task BuyerCancelling_Succeeds()
    {
        // The control for the refusal above: the same return and the same call, from its own buyer.
        _orderReturn = NewReturn(ReturnStatus.Requested);

        var result = await _service.Cancel(ReturnId, "Not needed", new ReturnFlowContext { CustomerId = BuyerId }, TestContext.Current.CancellationToken);

        Assert.Equal(ReturnStatus.Cancelled, result.Status);
        Assert.Same(result, Assert.Single(_saved));
    }

    [Fact]
    public async Task ColleagueEditingADraft_IsRefusedAsNotFound()
    {
        _orderReturn = NewReturn(ReturnStatus.Draft);

        var request = new UpdateReturnRequest { ReturnId = ReturnId, CustomerComment = "Taking this over" };
        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => _service.UpdateDraft(request, Colleague(), TestContext.Current.CancellationToken));

        Assert.Equal(ReturnFlowError.ReturnNotFound, exception.Code);
        Assert.Empty(_saved);
    }

    [Fact]
    public async Task ColleagueSubmittingADraft_IsRefusedAsNotFound()
    {
        _orderReturn = NewReturn(ReturnStatus.Draft);

        var exception = await Assert.ThrowsAsync<ReturnFlowException>(() => _service.Submit(ReturnId, Colleague(), TestContext.Current.CancellationToken));

        Assert.Equal(ReturnFlowError.ReturnNotFound, exception.Code);
        Assert.Empty(_saved);
    }

    private static ReturnFlowContext Colleague()
    {
        return new ReturnFlowContext { CustomerId = ColleagueId };
    }

    private static Return NewReturn(string status)
    {
        return new Return { Id = ReturnId, CustomerId = BuyerId, OrganizationId = "org-1", Status = status };
    }
}
