using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models.Search
{
    public class ReturnSearchCriteria : SearchCriteriaBase
    {
        public string OrderId { get; set; }

        /// <summary>
        /// Restricts the search to returns raised against these orders.
        /// </summary>
        /// <remarks>
        /// This is how "my returns" is expressed for now: <see cref="Return"/> carries no customer
        /// of its own, so the caller resolves the buyer's orders first. Once the domain model gains
        /// Return.CustomerId this is replaced by a direct filter.
        /// </remarks>
        public IList<string> OrderIds { get; set; }

        public IList<string> Statuses { get; set; }
    }
}
