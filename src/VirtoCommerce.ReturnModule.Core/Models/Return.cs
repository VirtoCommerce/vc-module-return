using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models
{
    public class Return : AuditableEntity, ICloneable
    {
        public string Number { get; set; }

        public string OrderId { get; set; }

        public string Status { get; set; }

        public CustomerOrder Order { get; set; }

        public ICollection<ReturnLineItem> LineItems { get; set; }

        #region ICloneable Members

        public virtual object Clone()
        {
            var result = MemberwiseClone() as Return;
            result.LineItems = LineItems?.Select(x => x.Clone()).OfType<ReturnLineItem>().ToList();

            return result;
        }

        #endregion ICloneable Members
    }
}
