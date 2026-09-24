using System;
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
        private readonly IReturnFlowService _returnFlowService;

        public ReturnController(IReturnSearchService returnSearchService, IReturnService returnService, IReturnFlowService returnFlowService)
        {
            _returnSearchService = returnSearchService;
            _returnService = returnService;
            _returnFlowService = returnFlowService;
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
            var storedReturn = string.IsNullOrEmpty(orderReturn.Id)
                ? null
                : await _returnService.GetByIdAsync(orderReturn.Id, ReturnResponseGroup.None.ToString());

            var errors = ValidateStatusChange(storedReturn, orderReturn)
                .Concat(await ValidateReturn(orderReturn))
                .ToList();

            if (errors.Count > 0)
            {
                return BadRequest(errors);
            }

            KeepDecisions(orderReturn, storedReturn);

            await _returnService.SaveChangesAsync(new[] { orderReturn });

            return Ok(new { orderReturn.Id });
        }

        /// <summary>
        /// Approve, partly approve or decline a requested return, line by line
        /// </summary>
        [HttpPost]
        [Route("{id}/authorize")]
        [Authorize(ModuleConstants.Security.Permissions.Authorize)]
        public async Task<ActionResult<Return>> AuthorizeReturn(string id, [FromBody] ReturnAuthorizationRequest request)
        {
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
                .Select(x => $"LineItem {x.OrderLineItemId} has incorrect quantity")
                .ToList();
        }

        // Written by the return flow only: the buyer submits and cancels, the agent authorizes. A
        // status set here would skip the checks those make, and the decision the status stands for.
        private static readonly ISet<string> _flowStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ReturnStatus.Draft,
            ReturnStatus.Requested,
            ReturnStatus.PartiallyApproved,
            ReturnStatus.Rejected,
        };

        private static IEnumerable<string> ValidateStatusChange(Return storedReturn, Return orderReturn)
        {
            var oldStatus = storedReturn?.Status ?? string.Empty;
            var newStatus = orderReturn.Status ?? string.Empty;

            if (!oldStatus.EqualsIgnoreCase(newStatus) && (_flowStatuses.Contains(oldStatus) || _flowStatuses.Contains(newStatus)))
            {
                yield return $"Status '{oldStatus}' cannot be changed to '{newStatus}' here: it is set by submitting, cancelling or authorizing the return.";
            }
        }

        // The decision is recorded by authorizing the return; an edit keeps whatever was decided.
        private static void KeepDecisions(Return orderReturn, Return storedReturn)
        {
            orderReturn.RejectReason = storedReturn?.RejectReason;

            foreach (var lineItem in orderReturn.LineItems)
            {
                var storedLineItem = storedReturn?.LineItems.FirstOrDefault(x => x.Id.EqualsIgnoreCase(lineItem.Id));

                lineItem.ApprovedQuantity = storedLineItem?.ApprovedQuantity ?? 0;
                lineItem.RejectReason = storedLineItem?.RejectReason;
                lineItem.ItemState = storedLineItem?.ItemState;
            }
        }
    }
}
