using VirtoCommerce.Return.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace VirtoCommerce.Return.Web.Controllers.Api
{
    [Route("api/Return")]
    public class ReturnController : Controller
    {
        // GET: api/VirtoCommerce.Return
        /// <summary>
        /// Get message
        /// </summary>
        /// <remarks>Return "Hello world!" message</remarks>
        [HttpGet]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public ActionResult<string> Get()
        {
            return Ok(new { result = "Hello world!" });
        }
    }
}
