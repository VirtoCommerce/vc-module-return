using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models
{
    public class ReturnLineItem : AuditableEntity, ICloneable
    {
        public string ReturnId { get; set; }

        public string OrderLineItemId { get; set; }

        #region Snapshots taken from the order line

        public string ProductId { get; set; }

        public string Sku { get; set; }

        public string Name { get; set; }

        public string ImageUrl { get; set; }

        public string MeasureUnit { get; set; }

        /// <summary>
        /// Quantity on the order line at the moment the return was raised.
        /// </summary>
        public int OrderedQuantity { get; set; }

        /// <summary>
        /// Negotiated price from the order line, never re-read from the catalog: B2B contract
        /// prices must not drift after the fact.
        /// </summary>
        public decimal Price { get; set; }

        #endregion

        /// <summary>
        /// Quantity the buyer asked to return.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Quantity an agent authorized; 0 means the line was rejected. Not reachable until
        /// approving is implemented, but persisted from the start to avoid a second migration.
        /// </summary>
        public int ApprovedQuantity { get; set; }

        /// <summary>
        /// Computed on read, never stored.
        /// </summary>
        public int AvailableQuantity { get; set; }

        public string ItemState { get; set; }

        /// <summary>
        /// Code from the store's Return.Reasons dictionary.
        /// </summary>
        public string ReasonCode { get; set; }

        public string ReasonComment { get; set; }

        /// <summary>
        /// Free-text reason from the original module, kept alongside <see cref="ReasonCode"/> so
        /// existing rows do not lose what was written in them.
        /// </summary>
        public string Reason { get; set; }

        public string RejectReason { get; set; }

        public string SerialNumber { get; set; }

        /// <summary>
        /// Photos and documents backing this line. Files live in the platform's file storage; these
        /// rows keep the reference, mirroring how quotes do it.
        /// </summary>
        public ICollection<ReturnAttachment> Attachments { get; set; }

        #region ICloneable members

        public virtual object Clone()
        {
            var result = MemberwiseClone() as ReturnLineItem;
            result.Attachments = Attachments?.Select(x => x.Clone()).OfType<ReturnAttachment>().ToList();

            return result;
        }

        #endregion ICloneable members
    }
}
