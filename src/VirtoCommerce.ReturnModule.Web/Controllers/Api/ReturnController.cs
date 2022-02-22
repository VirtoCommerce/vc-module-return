using VirtoCommerce.ReturnModule.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VirtoCommerce.ReturnModule.Core.Models.Search;
using VirtoCommerce.ReturnModule.Core.Services;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Web.Controllers.Api
{
    [Route("api/return")]
    public class ReturnController : Controller
    {
        private readonly IReturnSearchService _returnSearchService;

        public ReturnController(IReturnSearchService returnSearchService)
        {
            _returnSearchService = returnSearchService;
        }

        /// <summary>
        /// Get return list
        /// </summary>
        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<Return>> GetReturns([FromBody] ReturnSearchCriteria criteria)
        {
            var result = await _returnSearchService.SearchAsync(criteria);

            return Ok(result);
        }
    }
}
