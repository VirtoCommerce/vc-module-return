using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ReturnModule.Core.Models
{
    /// <summary>
    /// A photo or document attached to one returned line. Uploaded through FileExperienceApi;
    /// this row keeps the reference so the return stays readable without the file module.
    /// </summary>
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
