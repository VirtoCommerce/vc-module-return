using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Data.Handlers;
using Xunit;
using WebModule = VirtoCommerce.ReturnModule.Web.Module;

namespace VirtoCommerce.ReturnModule.Tests;

/// <summary>
/// The bus keeps its own handler list and never looks in the container, so a handler that is
/// registered in DI but not subscribed is silently never called. This takes the registrations from
/// Module.Initialize itself and the subscriptions from Module.RegisterEventHandlers, and drives a saved
/// return through a real bus. That PostInitialize calls RegisterEventHandlers is not covered here: it
/// needs the whole platform to run.
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Initialize_RegistersTheHandlersItSubscribes(bool withPushMessages)
    {
        var services = Initialize(withPushMessages);

        Assert.Contains(services, x => x.ServiceType == typeof(ReturnStatusChangedEventPublisher));
        Assert.Contains(services, x => x.ServiceType == typeof(SendNotificationsReturnStatusChangedEventHandler));
        Assert.Equal(withPushMessages, services.Any(x => x.ServiceType == typeof(SendPushMessagesReturnStatusChangedEventHandler)));
    }

    [Fact]
    public void Initialize_PushMessagesInstalledButFailed_LeavesPushOut()
    {
        var pushMessages = new ManifestModuleInfo { IsInstalled = true };
        pushMessages.Errors.Add("Incompatible with this platform version");

        var services = Initialize(pushMessages);

        Assert.DoesNotContain(services, x => x.ServiceType == typeof(SendPushMessagesReturnStatusChangedEventHandler));
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

    private static IServiceCollection Initialize(bool withPushMessages)
    {
        return Initialize(withPushMessages ? new ManifestModuleInfo { IsInstalled = true } : null);
    }

    private static IServiceCollection Initialize(ManifestModuleInfo pushMessages)
    {
        var moduleService = new Mock<IModuleService>();
        moduleService.Setup(x => x.GetModule("VirtoCommerce.PushMessages")).Returns(pushMessages);

        var module = new WebModule
        {
            ModuleInfo = new ManifestModuleInfo(),
            Configuration = new ConfigurationBuilder().Build(),
            ModuleService = moduleService.Object,
        };

        var services = new ServiceCollection();
        module.Initialize(services);

        return services;
    }

    private ServiceProvider Wire(bool withPushMessages)
    {
        IServiceCollection services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<InProcessBus>();
        services.AddSingleton<IEventHandlerRegistrar>(x => x.GetRequiredService<InProcessBus>());
        services.AddSingleton<IEventPublisher>(x => x.GetRequiredService<InProcessBus>());

        // What Initialize registered for the handlers, with the channels themselves swapped for
        // recorders: they are covered elsewhere, and here only reaching them matters.
        foreach (var descriptor in Initialize(withPushMessages).Where(x => typeof(IEventHandler<ReturnStatusChangedEvent>).IsAssignableFrom(x.ServiceType) || x.ServiceType == typeof(ReturnStatusChangedEventPublisher)))
        {
            services.Add(descriptor);
        }

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
