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

        protected override async Task BeforeSaveChanges(IList<Return> returns)
        {
            if (returns.IsNullOrEmpty())
            {
                return;
            }

            var ordersById = (await GetOrdersForReturns(returns)).ToDictionary(x => x.Id);

            await EnsureEachReturnHasNumber(returns, ordersById);
            FillMissingSnapshots(returns, ordersById);
        }


        private async Task EnsureEachReturnHasNumber(IEnumerable<Return> returns, IDictionary<string, CustomerOrder> ordersById)
        {
            var returnsWithoutNumber = returns.Where(x => string.IsNullOrEmpty(x.Number)).ToList();
            if (returnsWithoutNumber.IsNullOrEmpty())
            {
                return;
            }

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

        /// <summary>
        /// Copies ownership and product details from the order onto anything that has none.
        /// </summary>
        /// <remarks>
        /// The admin UI still saves returns through the original REST contract, which knows nothing
        /// about these fields, so without this a return raised by an agent would reach the storefront
        /// with no product name and no owner. Only empty values are touched — a snapshot already
        /// taken must never be refreshed from the order, or a renamed product or a corrected price
        /// would silently rewrite history.
        /// </remarks>
        private static void FillMissingSnapshots(IEnumerable<Return> returns, IDictionary<string, CustomerOrder> ordersById)
        {
            foreach (var orderReturn in returns)
            {
                if (!ordersById.TryGetValue(orderReturn.OrderId ?? string.Empty, out var order))
                {
                    continue;
                }

                orderReturn.StoreId ??= order.StoreId;
                orderReturn.CustomerId ??= order.CustomerId;
                orderReturn.CustomerName ??= order.CustomerName;
                orderReturn.OrderNumber ??= order.Number;

                var orderLineItems = (order.Items ?? []).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

                foreach (var lineItem in orderReturn.LineItems ?? [])
                {
                    if (!string.IsNullOrEmpty(lineItem.Sku) ||
                        !orderLineItems.TryGetValue(lineItem.OrderLineItemId ?? string.Empty, out var orderLineItem))
                    {
                        continue;
                    }

                    lineItem.ProductId ??= orderLineItem.ProductId;
                    lineItem.Sku = orderLineItem.Sku;
                    lineItem.Name ??= orderLineItem.Name;
                    lineItem.ImageUrl ??= orderLineItem.ImageUrl;
                    lineItem.MeasureUnit ??= orderLineItem.MeasureUnit;
                    lineItem.ItemState ??= ReturnItemState.Requested;

                    if (lineItem.OrderedQuantity == 0)
                    {
                        lineItem.OrderedQuantity = orderLineItem.Quantity;
                    }

                    if (lineItem.Price == 0)
                    {
                        lineItem.Price = orderLineItem.Price;
                    }
                }
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
