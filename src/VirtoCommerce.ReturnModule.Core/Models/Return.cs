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

        /// <summary>
        /// Buyer who raised the return, copied from the order.
        /// </summary>
        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string OrderId { get; set; }

        public string OrderNumber { get; set; }

        /// <summary>
        /// Buyer's own purchase order reference, prefilled from the order where present.
        /// </summary>
        public string CustomerReference { get; set; }

        public string Status { get; set; }

        /// <summary>
        /// Free-text outcome kept from the original module. The spec drops it in favour of a typed
        /// resolution, but dropping a column loses whatever customers already wrote there, so it
        /// stays until repairs (VP-9241) settle what replaces it.
        /// </summary>
        public string Resolution { get; set; }

        /// <summary>
        /// Buyer's comment on the whole return.
        /// </summary>
        public string CustomerComment { get; set; }

        /// <summary>
        /// Internal comment, never shown to the buyer.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Header-level reject reason, used when every line is rejected.
        /// </summary>
        public string RejectReason { get; set; }

        /// <summary>
        /// Why the buyer withdrew the return. Kept apart from <see cref="RejectReason"/>, which is
        /// the agent's word on a return they refused — the two read the same to a machine and mean
        /// opposite things to a person.
        /// </summary>
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
