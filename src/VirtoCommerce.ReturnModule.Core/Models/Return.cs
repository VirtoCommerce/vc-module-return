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

        public string StoreId { get; set; }

        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerReference { get; set; }

        /// <summary>
        /// Culture the buyer was using when the return was raised. Notifications are rendered in it,
        /// which is why it is a snapshot: the buyer's current preference may have moved on since.
        /// </summary>
        public string LanguageCode { get; set; }

        public string Status { get; set; }

        public string Resolution { get; set; }

        public string CustomerComment { get; set; }

        public string Comment { get; set; }

        public string RejectReason { get; set; }

        public string CancelReason { get; set; }

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
