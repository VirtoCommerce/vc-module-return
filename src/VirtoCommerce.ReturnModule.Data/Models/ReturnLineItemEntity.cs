using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Data.Models
{
    public class ReturnLineItemEntity : AuditableEntity, IDataEntity<ReturnLineItemEntity, ReturnLineItem>
    {
        [Required]
        [StringLength(128)]
        public string ReturnId { get; set; }

        public virtual ReturnEntity Return { get; set; }

        [Required]
        [StringLength(128)]
        public string OrderLineItemId { get; set; }

        [StringLength(128)]
        public string ProductId { get; set; }

        [StringLength(64)]
        public string Sku { get; set; }

        [StringLength(255)]
        public string Name { get; set; }

        [StringLength(2048)]
        public string ImageUrl { get; set; }

        [StringLength(32)]
        public string MeasureUnit { get; set; }

        [Column(TypeName = "Money")]
        public decimal Price { get; set; }

        public int OrderedQuantity { get; set; }

        public int Quantity { get; set; }

        public int ApprovedQuantity { get; set; }

        [StringLength(64)]
        public string ItemState { get; set; }

        [StringLength(64)]
        public string ReasonCode { get; set; }

        [StringLength(1024)]
        public string ReasonComment { get; set; }

        [StringLength(1024)]
        public string Reason { get; set; }

        [StringLength(1024)]
        public string RejectReason { get; set; }

        [StringLength(128)]
        public string SerialNumber { get; set; }

        public virtual ObservableCollection<ReturnAttachmentEntity> Attachments { get; set; } = new NullCollection<ReturnAttachmentEntity>();

        public ReturnLineItem ToModel(ReturnLineItem model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.ReturnId = ReturnId;
            model.OrderLineItemId = OrderLineItemId;
            model.ProductId = ProductId;
            model.Sku = Sku;
            model.Name = Name;
            model.ImageUrl = ImageUrl;
            model.MeasureUnit = MeasureUnit;
            model.Price = Price;
            model.OrderedQuantity = OrderedQuantity;
            model.Quantity = Quantity;
            model.ApprovedQuantity = ApprovedQuantity;
            model.ItemState = ItemState;
            model.ReasonCode = ReasonCode;
            model.ReasonComment = ReasonComment;
            model.Reason = Reason;
            model.RejectReason = RejectReason;
            model.SerialNumber = SerialNumber;

            model.Attachments = Attachments.Select(x => x.ToModel(AbstractTypeFactory<ReturnAttachment>.TryCreateInstance())).ToList();

            return model;
        }

        public ReturnLineItemEntity FromModel(ReturnLineItem model, PrimaryKeyResolvingMap pkMap)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            ReturnId = model.ReturnId;
            OrderLineItemId = model.OrderLineItemId;
            ProductId = model.ProductId;
            Sku = model.Sku;
            Name = model.Name;
            ImageUrl = model.ImageUrl;
            MeasureUnit = model.MeasureUnit;
            Price = model.Price;
            OrderedQuantity = model.OrderedQuantity;
            Quantity = model.Quantity;
            ApprovedQuantity = model.ApprovedQuantity;
            ItemState = model.ItemState;
            ReasonCode = model.ReasonCode;
            ReasonComment = model.ReasonComment;
            Reason = model.Reason;
            RejectReason = model.RejectReason;
            SerialNumber = model.SerialNumber;

            if (model.Attachments != null)
            {
                Attachments = new ObservableCollection<ReturnAttachmentEntity>(model.Attachments
                    .Select(x => AbstractTypeFactory<ReturnAttachmentEntity>.TryCreateInstance().FromModel(x, pkMap))
                    .OfType<ReturnAttachmentEntity>());
            }

            return this;
        }

        public void Patch(ReturnLineItemEntity target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.ReturnId = ReturnId;
            target.OrderLineItemId = OrderLineItemId;
            target.ProductId = ProductId;
            target.Sku = Sku;
            target.Name = Name;
            target.ImageUrl = ImageUrl;
            target.MeasureUnit = MeasureUnit;
            target.Price = Price;
            target.OrderedQuantity = OrderedQuantity;
            target.Quantity = Quantity;
            target.ApprovedQuantity = ApprovedQuantity;
            target.ItemState = ItemState;
            target.ReasonCode = ReasonCode;
            target.ReasonComment = ReasonComment;
            target.Reason = Reason;
            target.RejectReason = RejectReason;
            target.SerialNumber = SerialNumber;

            if (!Attachments.IsNullCollection())
            {
                Attachments.Patch(target.Attachments, (sourceItem, targetItem) => sourceItem.Patch(targetItem));
            }
        }
    }
}
