using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models
{
    public class ReturnAttachment : AuditableEntity, ICloneable
    {
        public string ReturnLineItemId { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string MimeType { get; set; }

        public long Size { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
