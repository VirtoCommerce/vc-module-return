using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.ReturnModule.Core.Models;
using static VirtoCommerce.Platform.Data.Infrastructure.DbContextBase;

namespace VirtoCommerce.ReturnModule.Data.Models
{
    public class ReturnAttachmentEntity : AuditableEntity, IDataEntity<ReturnAttachmentEntity, ReturnAttachment>
    {
        [Required]
        [StringLength(IdLength)]
        public string ReturnLineItemId { get; set; }

        public virtual ReturnLineItemEntity ReturnLineItem { get; set; }

        [Required]
        [StringLength(Length1024)]
        public string Name { get; set; }

        [Required]
        [StringLength(UrlLength)]
        public string Url { get; set; }

        [StringLength(Length128)]
        public string MimeType { get; set; }

        public long Size { get; set; }

        public ReturnAttachment ToModel(ReturnAttachment model)
        {
            ArgumentNullException.ThrowIfNull(model);

            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.ReturnLineItemId = ReturnLineItemId;
            model.Name = Name;
            model.Url = Url;
            model.MimeType = MimeType;
            model.Size = Size;

            return model;
        }

        public ReturnAttachmentEntity FromModel(ReturnAttachment model, PrimaryKeyResolvingMap pkMap)
        {
            ArgumentNullException.ThrowIfNull(model);

            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            ReturnLineItemId = model.ReturnLineItemId;
            Name = model.Name;
            Url = model.Url;
            MimeType = model.MimeType;
            Size = model.Size;

            return this;
        }

        public void Patch(ReturnAttachmentEntity target)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.Name = Name;
            target.Url = Url;
            target.MimeType = MimeType;
            target.Size = Size;
        }
    }
}
