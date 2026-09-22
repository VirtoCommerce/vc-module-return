using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Handlers;
using Xunit;

namespace VirtoCommerce.ReturnModule.Tests;

public class ReturnStatusChangedEventPublisherTests
{
    private readonly List<ReturnStatusChangedEvent> _published = [];
    private readonly ReturnStatusChangedEventPublisher _publisher;

    public ReturnStatusChangedEventPublisherTests()
    {
        var eventPublisher = new Mock<IEventPublisher>();

        eventPublisher
            .Setup(x => x.Publish(It.IsAny<ReturnStatusChangedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ReturnStatusChangedEvent, CancellationToken>((@event, _) => _published.Add(@event))
            .Returns(Task.CompletedTask);

        _publisher = new ReturnStatusChangedEventPublisher(eventPublisher.Object);
    }

    [Fact]
    public async Task Handle_StatusUnchanged_PublishesNothing()
    {
        await Handle(Modified(ReturnStatus.Requested, ReturnStatus.Requested));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task Handle_StatusMoved_PublishesFromAndTo()
    {
        await Handle(Modified(ReturnStatus.Requested, ReturnStatus.Approved));

        var @event = Assert.Single(_published);
        Assert.Equal(ReturnStatus.Requested, @event.FromStatus);
        Assert.Equal(ReturnStatus.Approved, @event.ToStatus);
        Assert.Equal(ReturnStatus.Approved, @event.Return.Status);
    }

    [Fact]
    public async Task Handle_DraftSubmitted_PublishesRequested()
    {
        await Handle(Modified(ReturnStatus.Draft, ReturnStatus.Requested));

        var @event = Assert.Single(_published);
        Assert.Equal(ReturnStatus.Draft, @event.FromStatus);
        Assert.Equal(ReturnStatus.Requested, @event.ToStatus);
    }

    [Fact]
    public async Task Handle_LegacyCancelledSpelling_IsNotAStatusChange()
    {
        await Handle(Modified("Canceled", ReturnStatus.Cancelled));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task Handle_StatusDiffersOnlyInCase_IsNotAStatusChange()
    {
        await Handle(Modified(ReturnStatus.Requested, "requested"));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task Handle_ReturnCreatedAsRequested_PublishesWithoutFromStatus()
    {
        await Handle(Added(ReturnStatus.Requested));

        var @event = Assert.Single(_published);
        Assert.Null(@event.FromStatus);
        Assert.Equal(ReturnStatus.Requested, @event.ToStatus);
    }

    [Fact]
    public async Task Handle_DraftCreated_PublishesNothing()
    {
        await Handle(Added(ReturnStatus.Draft));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task Handle_DeletedReturn_PublishesNothing()
    {
        await Handle(new GenericChangedEntry<Return>(NewReturn(ReturnStatus.Requested), EntryState.Deleted));

        Assert.Empty(_published);
    }

    [Fact]
    public async Task Handle_SeveralEntries_PublishesOnlyForTheOnesThatMoved()
    {
        await Handle(
            Modified(ReturnStatus.Requested, ReturnStatus.Approved),
            Modified(ReturnStatus.Requested, ReturnStatus.Requested),
            Modified(ReturnStatus.Requested, ReturnStatus.Rejected));

        Assert.Equal(2, _published.Count);
        Assert.Equal(ReturnStatus.Approved, _published[0].ToStatus);
        Assert.Equal(ReturnStatus.Rejected, _published[1].ToStatus);
    }

    private Task Handle(params GenericChangedEntry<Return>[] changedEntries)
    {
        return _publisher.Handle(new ReturnChangedEvent(changedEntries));
    }

    private static GenericChangedEntry<Return> Modified(string oldStatus, string newStatus)
    {
        return new GenericChangedEntry<Return>(NewReturn(newStatus), NewReturn(oldStatus), EntryState.Modified);
    }

    private static GenericChangedEntry<Return> Added(string status)
    {
        // The single-argument constructor is what CrudService uses for an addition: both slots hold
        // the same instance, so a publisher that only compared statuses would stay silent.
        return new GenericChangedEntry<Return>(NewReturn(status), EntryState.Added);
    }

    private static Return NewReturn(string status)
    {
        return new Return { Id = "return-1", Number = "RET260922-00001", Status = status };
    }
}
