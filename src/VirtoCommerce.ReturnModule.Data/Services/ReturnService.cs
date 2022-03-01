using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.ReturnModule.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.ReturnModule.Data.Repositories;

namespace VirtoCommerce.ReturnModule.Data.Services
{
    public class ReturnService : CrudService<Return, ReturnEntity, ReturnChangingEvent, ReturnChangedEvent>, IReturnService
    {
        private readonly ICrudService<CustomerOrder> _crudOrderService;

        public ReturnService(Func<IReturnRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            ICrudService<CustomerOrder> crudOrderService)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _crudOrderService = crudOrderService;
        }

        protected override async Task<IEnumerable<ReturnEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return await ((IReturnRepository)repository).GetReturnsByIdsAsync(ids, responseGroup);
        }

        public override async Task<IReadOnlyCollection<Return>> GetAsync(List<string> ids, string responseGroup = null)
        {
            var returns = await base.GetAsync(ids, responseGroup);

            var returnResponseGroup = EnumUtility.SafeParseFlags(responseGroup, ReturnResponseGroup.WithOrders);

            if (returnResponseGroup.HasFlag(ReturnResponseGroup.WithOrders))
            {
                var orderIds = returns.Select(x => x.OrderId).Distinct().ToList();
                var orders = await _crudOrderService.GetAsync(orderIds);

                foreach (var orderReturn in returns)
                {
                    orderReturn.Order = orders.FirstOrDefault(x => x.Id == orderReturn.OrderId);
                }
            }

            return returns;
        }
    }
}
