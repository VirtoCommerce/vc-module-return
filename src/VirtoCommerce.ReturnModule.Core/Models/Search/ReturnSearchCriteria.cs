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
        public IList<string> OrderIds { get; set; }

        /// <summary>
        /// The buyer who raised the return. This is what "my returns" means.
        /// </summary>
        /// <remarks>
        /// Returns saved before this field existed carry no customer, so they match nothing here.
        /// ReturnService fills it from the order whenever a return is saved.
        /// </remarks>
        public string CustomerId { get; set; }

        public IList<string> Statuses { get; set; }
    }
}
