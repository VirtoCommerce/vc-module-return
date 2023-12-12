using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Services
{
    public class ReturnService : CrudService<Return, ReturnEntity, ReturnChangingEvent, ReturnChangedEvent>, IReturnService
    {
        private readonly Func<IReturnRepository> _repositoryFactory;
        private readonly ICustomerOrderService _orderService;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly IStoreService _storeService;
        private readonly ISettingsManager _settingsManager;

        public ReturnService(
            Func<IReturnRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            ICustomerOrderService orderService,
            IUniqueNumberGenerator uniqueNumberGenerator,
            IStoreService storeService,
            ISettingsManager settingsManager)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _repositoryFactory = repositoryFactory;
            _orderService = orderService;
            _uniqueNumberGenerator = uniqueNumberGenerator;
            _storeService = storeService;
            _settingsManager = settingsManager;
        }

        public override async Task<IList<Return>> GetAsync(IList<string> ids, string responseGroup = null, bool clone = true)
        {
            var returnResponseGroup = EnumUtility.SafeParseFlags(responseGroup, ReturnResponseGroup.WithOrders);
            var withOrders = returnResponseGroup.HasFlag(ReturnResponseGroup.WithOrders);
            clone |= withOrders;

            var returns = await base.GetAsync(ids, responseGroup, clone);

            if (withOrders && returns.Any())
            {
                var orders = await GetOrdersForReturns(returns);

                foreach (var orderReturn in returns)
                {
                    orderReturn.Order = orders.FirstOrDefault(x => x.Id == orderReturn.OrderId);
                }
            }

            return returns;
        }

        public virtual async Task<Dictionary<string, int>> GetItemsAvailableQuantities(string orderId)
        {
            var order = await _orderService.GetByIdAsync(orderId);

            return await GetItemsAvailableQuantities(order);
        }

        public virtual async Task<Dictionary<string, int>> GetItemsAvailableQuantities(CustomerOrder order, string returnId = null)
        {
            using var repository = _repositoryFactory();
            var returnIds = repository.Returns
                .Where(x => x.OrderId == order.Id)
                .Select(x => x.Id);

            if (returnId != null)
            {
                returnIds = returnIds.Where(x => x != returnId);
            }

            var returns = await GetAsync(returnIds.ToList());

            var result = order.Items
                .Select(lineItem => new KeyValuePair<string, int>(
                    lineItem.Id,
                    Math.Max(0, lineItem.Quantity - returns
                        .SelectMany(x => x.LineItems)
                        .Where(x => x.OrderLineItemId == lineItem.Id)
                        .Sum(x => x.Quantity))))
                .ToDictionary(k => k.Key, v => v.Value);

            return result;
        }


        protected override Task<IList<ReturnEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((IReturnRepository)repository).GetReturnsByIdsAsync(ids, responseGroup);
        }

        protected override Task BeforeSaveChanges(IList<Return> returns)
        {
            return EnsureEachReturnHasNumber(returns);
        }


        private async Task EnsureEachReturnHasNumber(IEnumerable<Return> returns)
        {
            var returnsWithoutNumber = returns.Where(x => string.IsNullOrEmpty(x.Number)).ToList();
            if (returnsWithoutNumber.IsNullOrEmpty())
            {
                return;
            }

            var ordersById = (await GetOrdersForReturns(returnsWithoutNumber)).ToDictionary(x => x.Id);
            var storeIds = ordersById.Values.Select(x => x.StoreId).Distinct().ToList();
            var storesById = (await _storeService.GetNoCloneAsync(storeIds)).ToDictionary(x => x.Id);

            var settingDescriptor = ModuleConstants.Settings.General.ReturnNewNumberTemplate;
            var globalNumberTemplate = await _settingsManager.GetValueAsync<string>(settingDescriptor);

            foreach (var orderReturn in returnsWithoutNumber)
            {
                var numberTemplate = globalNumberTemplate;

                if (ordersById.TryGetValue(orderReturn.OrderId, out var order) &&
                    storesById.TryGetValue(order.StoreId, out var store))
                {
                    numberTemplate = store.Settings.GetValue<string>(settingDescriptor);
                }

                orderReturn.Number = _uniqueNumberGenerator.GenerateNumber(numberTemplate);
            }
        }

        private async Task<IList<CustomerOrder>> GetOrdersForReturns(IEnumerable<Return> returns)
        {
            var orderIds = returns.Select(x => x.OrderId).Distinct().ToList();
            var orders = await _orderService.GetAsync(orderIds);

            return orders;
        }
    }
}
