using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models
{
    public class ReturnLineItem : AuditableEntity, ICloneable
    {
        public string ReturnId { get; set; }

        public string OrderLineItemId { get; set; }

        #region ICloneable members

        public virtual object Clone()
        {
            var result = MemberwiseClone() as ReturnLineItem;
            return result;
        }

        #endregion ICloneable members
    }
}
