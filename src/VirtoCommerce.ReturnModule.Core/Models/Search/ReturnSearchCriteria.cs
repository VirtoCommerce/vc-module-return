using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models.Search
{
    public class ReturnSearchCriteria : SearchCriteriaBase
    {
        public string OrderId { get; set; }

        public IList<string> OrderIds { get; set; }

        public string CustomerId { get; set; }

        public string OrganizationId { get; set; }

        /// <summary>
        /// When set, drafts are only found if this customer raised them: a draft is the buyer's own
        /// work in progress, not yet something the organization is waiting on.
        /// </summary>
        public string DraftsOfCustomerId { get; set; }

        public string StoreId { get; set; }

        public IList<string> Statuses { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
