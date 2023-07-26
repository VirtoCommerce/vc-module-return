using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Repositories;
using VirtoCommerce.ReturnModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ReturnModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<ReturnDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce"));
            });

            serviceCollection.AddTransient<IReturnRepository, ReturnRepositoryImpl>();
            serviceCollection.AddTransient<Func<IReturnRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<IReturnRepository>());
            serviceCollection.AddTransient<IReturnService, ReturnService>();
            serviceCollection.AddTransient<IReturnSearchService, ReturnSearchService>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            // Register settings
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));

            // Register permissions
            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Return", ModuleConstants.Security.Permissions.AllPermissions);

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
