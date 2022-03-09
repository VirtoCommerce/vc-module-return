using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ReturnModule.Data.Services
{
    public class ReturnService : CrudService<Return, ReturnEntity, ReturnChangingEvent, ReturnChangedEvent>,
        IReturnService
    {
        private readonly ICrudService<CustomerOrder> _crudOrderService;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly ICrudService<Store> _storeService;

        public ReturnService(Func<IReturnRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            ICrudService<CustomerOrder> crudOrderService,
            IUniqueNumberGenerator uniqueNumberGenerator,
            ICrudService<Store> storeService)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _crudOrderService = crudOrderService;
            _uniqueNumberGenerator = uniqueNumberGenerator;
            _storeService = storeService;
        }

        protected override async Task<IEnumerable<ReturnEntity>> LoadEntities(IRepository repository,
            IEnumerable<string> ids, string responseGroup)
        {
            return await ((IReturnRepository)repository).GetReturnsByIdsAsync(ids, responseGroup);
        }

        public override async Task<IReadOnlyCollection<Return>> GetAsync(List<string> ids, string responseGroup = null)
        {
            var returns = await base.GetAsync(ids, responseGroup);

            var returnResponseGroup = EnumUtility.SafeParseFlags(responseGroup, ReturnResponseGroup.WithOrders);

            if (returnResponseGroup.HasFlag(ReturnResponseGroup.WithOrders))
            {
                var orders = await GetOrdersForReturns(returns);

                foreach (var orderReturn in returns)
                {
                    orderReturn.Order = orders.FirstOrDefault(x => x.Id == orderReturn.OrderId);
                }
            }

            return returns;
        }

        public virtual async Task SaveChangesAsync(Return[] returns)
        {
            var pkMap = new PrimaryKeyResolvingMap();
            var changedEntries = new List<GenericChangedEntry<Return>>();
            var changedEntitiesMap = new Dictionary<Return, ReturnEntity>();

            var orders = await GetOrdersForReturns(returns);

            using (var repository = (IReturnRepository)_repositoryFactory())
            {
                var returnIds = returns.Where(x => !x.IsTransient()).Select(x => x.Id).ToArray();
                var dataExistReturns =
                    await repository.GetReturnsByIdsAsync(returnIds, ReturnResponseGroup.WithOrders.ToString());

                foreach (var modifiedReturn in returns)
                {
                    var originalEntity = dataExistReturns.FirstOrDefault(x => x.Id == modifiedReturn.Id);

                    if (originalEntity != null)
                    {
                        originalEntity.ModifiedDate = DateTime.UtcNow;

                        var modifiedEntity = AbstractTypeFactory<ReturnEntity>
                            .TryCreateInstance()
                            .FromModel(modifiedReturn, pkMap);

                        repository.TrackModifiedAsAddedForNewChildEntities(originalEntity);

                        changedEntries.Add(new GenericChangedEntry<Return>(modifiedReturn,
                            originalEntity.ToModel(AbstractTypeFactory<Return>.TryCreateInstance()),
                            EntryState.Modified));

                        modifiedEntity?.Patch(originalEntity);

                        var newModel = originalEntity.ToModel(AbstractTypeFactory<Return>.TryCreateInstance());

                        var newModifiedEntity = AbstractTypeFactory<ReturnEntity>
                            .TryCreateInstance()
                            .FromModel(newModel, pkMap);

                        newModifiedEntity?.Patch(originalEntity);
                        changedEntitiesMap.Add(modifiedReturn, originalEntity);
                    }
                    else
                    {
                        await SetReturnNumber(modifiedReturn, orders.FirstOrDefault(x => x.Id == modifiedReturn.OrderId)?.StoreId);

                        var modifiedEntity = AbstractTypeFactory<ReturnEntity>
                            .TryCreateInstance()
                            .FromModel(modifiedReturn, pkMap);

                        repository.Add(modifiedEntity);

                        changedEntries.Add(new GenericChangedEntry<Return>(modifiedReturn, EntryState.Added));
                        changedEntitiesMap.Add(modifiedReturn, modifiedEntity);
                    }
                }

                await _eventPublisher.Publish(new ReturnChangedEvent(changedEntries));
                await repository.UnitOfWork.CommitAsync();
                pkMap.ResolvePrimaryKeys();
                ClearCache(returns);
            }
        }

        private async Task SetReturnNumber(Return orderReturn, string storeId)
        {
            var store = await _storeService.GetByIdAsync(storeId);

            var numberTemplate = ModuleConstants.Settings.General.ReturnNewNumberTemplate.DefaultValue.ToString();
            if (store != null)
            {
                numberTemplate = store.Settings.GetSettingValue(ModuleConstants.Settings.General.ReturnNewNumberTemplate.Name, numberTemplate);
            }

            var number = _uniqueNumberGenerator.GenerateNumber(numberTemplate);

            orderReturn.Number = number;
        }

        private async Task<IReadOnlyCollection<CustomerOrder>> GetOrdersForReturns(IEnumerable<Return> returns)
        {
            var orderIds = returns.Select(x => x.OrderId).Distinct().ToList();
            var orders = await _crudOrderService.GetAsync(orderIds);

            return orders;
        }
    }
}
