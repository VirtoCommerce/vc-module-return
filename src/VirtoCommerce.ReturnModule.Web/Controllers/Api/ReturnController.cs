using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.Core;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Web.Controllers.Api
{
    [Route("api/return")]
    public class ReturnController : Controller
    {
        private readonly IReturnSearchService _returnSearchService;
        private readonly IReturnService _returnService;

        public ReturnController(IReturnSearchService returnSearchService, IReturnService returnService)
        {
            _returnSearchService = returnSearchService;
            _returnService = returnService;
        }

        /// <summary>
        /// Get return list
        /// </summary>
        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<Return>> SearchReturns([FromBody] ReturnSearchCriteria criteria)
        {
            var result = await _returnSearchService.SearchNoCloneAsync(criteria);
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

            await _returnService.SaveChangesAsync(new[] { orderReturn });

            return Ok(new { orderReturn.Id });
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
            var availableQuantities = orderReturn.Order == null
                ? await _returnService.GetItemsAvailableQuantities(orderReturn.OrderId)
                : await _returnService.GetItemsAvailableQuantities(orderReturn.Order, orderReturn.Id);

            return orderReturn.LineItems
                .Where(item => item.Quantity < 1 ||
                               item.Quantity > availableQuantities[item.OrderLineItemId])
                .Select(x => $"LineItem {x.OrderLineItemId} has incorrect quantity");
        }
    }
}
