using System;
using System.IO;
using FluentValidation;
using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.MySql.Extensions;
using VirtoCommerce.Platform.Data.PostgreSql.Extensions;
using VirtoCommerce.Platform.Data.SqlServer.Extensions;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Notifications;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Handlers;
using VirtoCommerce.ReturnModule.Data.MySql;
using VirtoCommerce.ReturnModule.Data.PostgreSql;
using VirtoCommerce.ReturnModule.Data.Repositories;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.ReturnModule.Data.SqlServer;
using VirtoCommerce.ReturnModule.Data.Validation;
using VirtoCommerce.ReturnModule.ExperienceApi;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;


namespace VirtoCommerce.ReturnModule.Web
{
    public class Module : IModule, IHasConfiguration, IHasModuleService
    {
        private const string PushMessagesModuleId = "VirtoCommerce.PushMessages";

        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }
        public IModuleService ModuleService { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            // Initialize database
            serviceCollection.AddDbContext<ReturnDbContext>((provider, options) =>
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

                switch (databaseProvider)
                {
                    case "MySql":
                        options.UseMySqlDatabase(connectionString, typeof(MySqlDataAssemblyMarker), Configuration);
                        break;
                    case "PostgreSql":
                        options.UsePostgreSqlDatabase(connectionString, typeof(PostgreSqlDataAssemblyMarker), Configuration);
                        break;
                    default:
                        options.UseSqlServerDatabase(connectionString, typeof(SqlServerDataAssemblyMarker), Configuration);
                        break;
                }
            });

            serviceCollection.AddTransient<IReturnRepository, ReturnRepositoryImpl>();
            serviceCollection.AddTransient<Func<IReturnRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<IReturnRepository>());
            serviceCollection.AddTransient<IReturnService, ReturnService>();
            serviceCollection.AddTransient<IReturnSearchService, ReturnSearchService>();
            serviceCollection.AddTransient<IReturnQuantityService, ReturnQuantityService>();
            serviceCollection.AddTransient<IReturnEligibilityService, ReturnEligibilityService>();
            serviceCollection.AddTransient<IReturnAttachmentService, ReturnAttachmentService>();
            serviceCollection.AddTransient<IReturnSettingsService, ReturnSettingsService>();
            serviceCollection.AddTransient<IReturnStateProvider, ReturnStateProvider>();
            serviceCollection.AddTransient<IReturnFlowService, ReturnFlowService>();
            serviceCollection.AddTransient<AbstractValidator<ReturnRequestValidationContext>, ReturnRequestValidator>();

            serviceCollection.AddTransient<IEventHandler<ReturnChangedEvent>, ReturnStatusChangedEventPublisher>();
            serviceCollection.AddTransient<IEventHandler<ReturnStatusChangedEvent>, SendNotificationsReturnStatusChangedEventHandler>();

            // The handler names IPushMessageService, so resolving it at all would load an assembly
            // that is not there when the optional module is not installed.
            if (ModuleService.IsInstalled(PushMessagesModuleId))
            {
                serviceCollection.AddTransient<IEventHandler<ReturnStatusChangedEvent>, SendPushMessagesReturnStatusChangedEventHandler>();
            }

            // GraphQL
            _ = new GraphQLBuilder(serviceCollection, builder =>
            {
                builder.AddSchema(serviceCollection, typeof(AssemblyMarker));
            });

            serviceCollection.AddSingleton<IAuthorizationHandler, ReturnAuthorizationHandler>();
            serviceCollection.AddSingleton<IFileAuthorizationRequirementFactory, ReturnFileAuthorizationRequirementFactory>();
            serviceCollection.AddSingleton<ScopedSchemaFactory<AssemblyMarker>>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            appBuilder.UseScopedSchema<AssemblyMarker>("return");

            // Register settings
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));

            // Register permissions
            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Return", ModuleConstants.Security.Permissions.AllPermissions);

            // Register notifications
            var notificationRegistrar = appBuilder.ApplicationServices.GetRequiredService<INotificationRegistrar>();
            var templatesPath = Path.Combine(ModuleInfo.FullPhysicalPath, "NotificationTemplates");

            notificationRegistrar.RegisterNotification<ReturnRegisteredEmailNotification>().WithTemplatesFromPath(templatesPath);
            notificationRegistrar.RegisterNotification<ReturnApprovedEmailNotification>().WithTemplatesFromPath(templatesPath);
            notificationRegistrar.RegisterNotification<ReturnPartiallyApprovedEmailNotification>().WithTemplatesFromPath(templatesPath);
            notificationRegistrar.RegisterNotification<ReturnRejectedEmailNotification>().WithTemplatesFromPath(templatesPath);

            // Apply migrations
            using var serviceScope = appBuilder.ApplicationServices.CreateScope();
            using var dbContext = serviceScope.ServiceProvider.GetRequiredService<ReturnDbContext>();
            dbContext.Database.Migrate();
        }

        public void Uninstall()
        {
            // do nothing in here
        }
    }
}
