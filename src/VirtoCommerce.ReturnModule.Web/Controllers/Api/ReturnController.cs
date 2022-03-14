using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ReturnModule.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Web.Controllers.Api
{
    [Route("api/return")]
    public class ReturnController : Controller
    {
        private readonly IReturnSearchService _returnSearchService;
        private readonly IReturnService _returnService;
        private readonly ICrudService<CustomerOrder> _crudOrderService;

        public ReturnController(IReturnSearchService returnSearchService, IReturnService returnService, ICrudService<CustomerOrder> crudOrderService)
        {
            _returnSearchService = returnSearchService;
            _returnService = returnService;
            _crudOrderService = crudOrderService;
        }

        /// <summary>
        /// Get return list
        /// </summary>
        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<Return>> SearchReturns([FromBody] ReturnSearchCriteria criteria)
        {
            var result = await _returnSearchService.SearchAsync(criteria);
            return Ok(result);
        }

        /// <summary>
        /// Find return by Id
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<Return>> GetReturnById(string id)
        {
            var result = await _returnService.GetByIdAsync(id);

            var availableQuantities = await _returnService.GetItemsAvailableQuantities(result.Order, id);

            foreach (var lineItem in result.LineItems)
            {
                lineItem.AvailableQuantity =
                    availableQuantities.FirstOrDefault(x => x.Key == lineItem.OrderLineItemId).Value;
            }

            return Ok(result);
        }

        /// <summary>
        /// Update return
        /// </summary>
        /// <param name="orderReturn"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult> UpdateReturn([FromBody] Return orderReturn)
        {
            var errors = await ValidateReturn(orderReturn);

            if (errors.Any())
            {
                return BadRequest(errors);
            }

            var result = await _returnService.SaveChangesAsync(new[] { orderReturn });

            return Ok(new {Id = result.FirstOrDefault()});
        }

        /// <summary>
        /// Delete return
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Delete)]
        public async Task<ActionResult> DeleteReturn([FromQuery] string[] ids)
        {
            await _returnService.DeleteAsync(ids);
            return NoContent();
        }

        /// <summary>
        /// Returns available item quantities for the order with passed ID
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("available-quantities/{orderId}")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<Dictionary<string, int>>> GetAvailableQuantities(string orderId)
        {
            var result = await _returnService.GetItemsAvailableQuantities(orderId);

            return Ok(result);
        }

        private async Task<IEnumerable<string>> ValidateReturn(Return orderReturn)
        {
            var order = await _crudOrderService.GetByIdAsync(orderReturn.OrderId);

            return orderReturn.LineItems
                .Where(returnLineItem => returnLineItem.Quantity < 1 ||
                                         returnLineItem.Quantity > order.Items.First(orderLiteItem =>
                                             orderLiteItem.Id == returnLineItem.OrderLineItemId).Quantity)
                .Select(x => $"LineItem {x.OrderLineItemId} has incorrect quantity");
        }
    }
}
