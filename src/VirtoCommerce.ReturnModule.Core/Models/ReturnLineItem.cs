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

        public int OrderedQuantity { get; set; }

        public decimal Price { get; set; }

        #endregion

        public int Quantity { get; set; }

        public int ApprovedQuantity { get; set; }

        public int AvailableQuantity { get; set; }

        public string ItemState { get; set; }

        public string ReasonCode { get; set; }

        public string ReasonComment { get; set; }

        public string Reason { get; set; }

        public string RejectReason { get; set; }

        public string SerialNumber { get; set; }

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
