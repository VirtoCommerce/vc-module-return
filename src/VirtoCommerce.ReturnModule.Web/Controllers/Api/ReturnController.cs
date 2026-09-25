using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
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
        private readonly IReturnFlowService _returnFlowService;
        private readonly IReturnStateProvider _stateProvider;
        private readonly IReturnQuantityService _quantityService;
        private readonly ICustomerOrderService _orderService;
        private readonly ILocalizableSettingService _localizableSettingService;

        public ReturnController(
            IReturnSearchService returnSearchService,
            IReturnService returnService,
            IReturnFlowService returnFlowService,
            IReturnStateProvider stateProvider,
            IReturnQuantityService quantityService,
            ICustomerOrderService orderService,
            ILocalizableSettingService localizableSettingService)
        {
            _returnSearchService = returnSearchService;
            _returnService = returnService;
            _returnFlowService = returnFlowService;
            _stateProvider = stateProvider;
            _quantityService = quantityService;
            _orderService = orderService;
            _localizableSettingService = localizableSettingService;
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

            if (result == null)
            {
                return NotFound();
            }

            var availableQuantities = await GetAvailableQuantitiesAsync(result.Order, id);

            foreach (var lineItem in result.LineItems)
            {
                lineItem.AvailableQuantity = availableQuantities.GetValueOrDefault(lineItem.OrderLineItemId ?? string.Empty);
            }

            return Ok(result);
        }

        /// <summary>
        /// Statuses an edit can give the return
        /// </summary>
        [HttpGet]
        [Route("{id}/available-statuses")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<string[]>> GetAvailableStatuses(string id)
        {
            var orderReturn = await _returnService.GetByIdAsync(id, ReturnResponseGroup.None.ToString());

            if (orderReturn == null)
            {
                return NotFound();
            }

            var statuses = await _localizableSettingService.GetValuesAsync(ModuleConstants.Settings.General.OrderStatus.Name, languageCode: null);

            return Ok(statuses
                .Select(x => x.Key)
                .Where(x => _stateProvider.CanSetStatus(orderReturn, x))
                .ToArray());
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
            if (orderReturn == null)
            {
                return BadRequest();
            }

            var storedReturn = string.IsNullOrEmpty(orderReturn.Id)
                ? null
                : await _returnService.GetByIdAsync(orderReturn.Id, ReturnResponseGroup.None.ToString());

            var errors = ValidateStatusChange(storedReturn, orderReturn)
                .Concat(ValidateLineChanges(storedReturn, orderReturn))
                .ToList();

            if (errors.Count == 0)
            {
                KeepDecisions(orderReturn, storedReturn);
                errors.AddRange(await ValidateReturn(orderReturn));
            }

            if (errors.Count > 0)
            {
                return BadRequest(errors);
            }

            await _returnService.SaveChangesAsync(new[] { orderReturn });

            return Ok(new { orderReturn.Id });
        }

        /// <summary>
        /// Approve, partly approve or decline a return, line by line
        /// </summary>
        [HttpPost]
        [Route("{id}/authorize")]
        [Authorize(ModuleConstants.Security.Permissions.Authorize)]
        public async Task<ActionResult<Return>> AuthorizeReturn(string id, [FromBody] ReturnAuthorizationRequest request)
        {
            if (request == null)
            {
                return BadRequest();
            }

            request.ReturnId = id;

            try
            {
                return Ok(await _returnFlowService.Authorize(request));
            }
            catch (ReturnFlowException ex) when (ex.Code == ReturnFlowError.ReturnNotFound)
            {
                return NotFound();
            }
            catch (ReturnFlowException ex)
            {
                return BadRequest(new { ex.Code, ex.Message });
            }
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
            var order = await _orderService.GetByIdAsync(orderId);

            return Ok(await GetAvailableQuantitiesAsync(order, excludeReturnId: null));
        }

        private async Task<IEnumerable<string>> ValidateReturn(Return orderReturn)
        {
            var order = orderReturn.Order ?? await _orderService.GetByIdAsync(orderReturn.OrderId);
            var availableQuantities = await GetAvailableQuantitiesAsync(order, orderReturn.Id);

            // Measured by what a line holds, as the other returns are: a decided line keeps the quantity
            // it was decided on, but holds only what was approved.
            return orderReturn.LineItems
                .Where(item => item.Quantity < 1 ||
                               _quantityService.GetHeldQuantity(orderReturn, item) > availableQuantities.GetValueOrDefault(item.OrderLineItemId ?? string.Empty))
                .Select(x => $"LineItem {x.OrderLineItemId} has incorrect quantity")
                .ToList();
        }

        // What the storefront offers too: the ordered quantity less what other returns hold, so a line
        // approved in part frees the rest here as well.
        private async Task<Dictionary<string, int>> GetAvailableQuantitiesAsync(CustomerOrder order, string excludeReturnId)
        {
            if (order == null)
            {
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }

            var heldQuantities = await _quantityService.GetHeldQuantities(order.Id, excludeReturnId);

            return order.Items.ToDictionary(
                x => x.Id,
                x => Math.Max(0, x.Quantity - (heldQuantities.TryGetValue(x.Id, out var held) ? held : 0)),
                StringComparer.OrdinalIgnoreCase);
        }

        private IEnumerable<string> ValidateStatusChange(Return storedReturn, Return orderReturn)
        {
            if (!_stateProvider.CanSetStatus(storedReturn, orderReturn.Status))
            {
                yield return $"Status '{storedReturn?.Status}' cannot be changed to '{orderReturn.Status}' by an edit: it is set by submitting, cancelling or authorizing the return.";
            }
        }

        // A decided return is the set of lines the decision was made on: a line added afterwards would
        // hold stock nobody approved, and a line dropped would take its decision with it.
        private IEnumerable<string> ValidateLineChanges(Return storedReturn, Return orderReturn)
        {
            if (storedReturn == null || !storedReturn.LineItems.Any(_stateProvider.IsDecided))
            {
                yield break;
            }

            var storedIds = storedReturn.LineItems.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newIds = orderReturn.LineItems.Select(x => x.Id ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!storedIds.SetEquals(newIds))
            {
                yield return $"Return '{storedReturn.Number}' has been approved or declined, so its lines cannot be added or removed.";
            }
        }

        // The decision is recorded by authorizing the return; an edit keeps whatever was decided,
        // including the requested quantity the approved one was measured against.
        private void KeepDecisions(Return orderReturn, Return storedReturn)
        {
            orderReturn.RejectReason = storedReturn?.RejectReason;

            foreach (var lineItem in orderReturn.LineItems)
            {
                var storedLineItem = storedReturn?.LineItems.FirstOrDefault(x => x.Id.EqualsIgnoreCase(lineItem.Id));

                lineItem.ApprovedQuantity = storedLineItem?.ApprovedQuantity ?? 0;
                lineItem.RejectReason = storedLineItem?.RejectReason;
                lineItem.ItemState = storedLineItem?.ItemState;

                if (storedLineItem != null && _stateProvider.IsDecided(storedLineItem))
                {
                    lineItem.Quantity = storedLineItem.Quantity;
                }
            }
        }
    }
}
