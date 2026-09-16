using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models.Search
{
    public class ReturnSearchCriteria : SearchCriteriaBase
    {
        public string OrderId { get; set; }

        public IList<string> OrderIds { get; set; }

        public string CustomerId { get; set; }

        public IList<string> Statuses { get; set; }
    }
}
