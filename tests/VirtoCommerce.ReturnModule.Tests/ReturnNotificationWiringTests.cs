using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Handlers;
using Xunit;
using WebModule = VirtoCommerce.ReturnModule.Web.Module;

namespace VirtoCommerce.ReturnModule.Tests;

/// <summary>
/// The bus keeps its own handler list and never looks in the container, so a handler that is
/// registered in DI but not subscribed is silently never called. This drives a saved return through
/// a real bus, wired the way the module wires it.
/// </summary>
public class ReturnNotificationWiringTests
{
    private readonly List<ReturnStatusChangedEvent> _emailed = [];
    private readonly List<ReturnStatusChangedEvent> _pushed = [];

    [Fact]
    public async Task StatusChange_ReachesBothChannelsThroughTheBus()
    {
        var provider = Wire(withPushMessages: true);

        await Publish(provider, ReturnStatus.Requested, ReturnStatus.Approved);

        Assert.Equal(ReturnStatus.Approved, Assert.Single(_emailed).ToStatus);
        Assert.Equal(ReturnStatus.Approved, Assert.Single(_pushed).ToStatus);
    }

    [Fact]
    public async Task PushMessagesAbsent_EmailStillSentAndPushHandlerNeverRegistered()
    {
        var provider = Wire(withPushMessages: false);

        await Publish(provider, ReturnStatus.Requested, ReturnStatus.Approved);

        Assert.Single(_emailed);
        Assert.Empty(_pushed);
        Assert.Null(provider.GetService<SendPushMessagesReturnStatusChangedEventHandler>());
    }

    private ServiceProvider Wire(bool withPushMessages)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<InProcessBus>();
        services.AddSingleton<IEventHandlerRegistrar>(x => x.GetRequiredService<InProcessBus>());
        services.AddSingleton<IEventPublisher>(x => x.GetRequiredService<InProcessBus>());

        WebModule.AddEventHandlers(services, withPushMessages);

        // The channels themselves are covered elsewhere; here only reaching them matters.
        services.AddTransient(_ => NewHandler<SendNotificationsReturnStatusChangedEventHandler>(_emailed, 8));

        if (withPushMessages)
        {
            services.AddTransient(_ => NewHandler<SendPushMessagesReturnStatusChangedEventHandler>(_pushed, 8));
        }

        var provider = services.BuildServiceProvider();

        WebModule.RegisterEventHandlers(new ApplicationBuilder(provider), withPushMessages);

        return provider;
    }

    private static T NewHandler<T>(List<ReturnStatusChangedEvent> received, int constructorArgumentCount)
        where T : ReturnStatusNotificationHandlerBase
    {
        var handler = new Mock<T>(new object[constructorArgumentCount]);

        handler
            .Setup(x => x.Handle(It.IsAny<ReturnStatusChangedEvent>()))
            .Callback<ReturnStatusChangedEvent>(received.Add)
            .Returns(Task.CompletedTask);

        return handler.Object;
    }

    private static Task Publish(ServiceProvider provider, string oldStatus, string newStatus)
    {
        var changedEntry = new GenericChangedEntry<Return>(NewReturn(newStatus), NewReturn(oldStatus), EntryState.Modified);

        return provider.GetRequiredService<IEventPublisher>().Publish(new ReturnChangedEvent([changedEntry]));
    }

    private static Return NewReturn(string status)
    {
        return new Return { Id = "return-1", Number = "RET260922-00001", Status = status };
    }
}
