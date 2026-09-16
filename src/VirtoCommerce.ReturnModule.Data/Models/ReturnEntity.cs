using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.ReturnModule.Core.Models;
using static VirtoCommerce.Platform.Data.Infrastructure.DbContextBase;

namespace VirtoCommerce.ReturnModule.Data.Models
{
    public class ReturnEntity : AuditableEntity, IDataEntity<ReturnEntity, Return>
    {
        [Required]
        [StringLength(Length64)]
        public string Number { get; set; }

        [Required]
        [StringLength(IdLength)]
        public string OrderId { get; set; }

        [StringLength(Length64)]
        public string OrderNumber { get; set; }

        [StringLength(IdLength)]
        public string StoreId { get; set; }

        [StringLength(IdLength)]
        public string CustomerId { get; set; }

        [StringLength(Length256)]
        public string CustomerName { get; set; }

        [StringLength(Length128)]
        public string CustomerReference { get; set; }

        [StringLength(Length64)]
        public string Status { get; set; }

        [StringLength(Length2048)]
        public string Resolution { get; set; }

        [StringLength(Length2048)]
        public string CustomerComment { get; set; }

        [StringLength(Length2048)]
        public string Comment { get; set; }

        [StringLength(Length2048)]
        public string RejectReason { get; set; }

        [StringLength(Length2048)]
        public string CancelReason { get; set; }

        public virtual ObservableCollection<ReturnLineItemEntity> LineItems { get; set; } = new NullCollection<ReturnLineItemEntity>();

        public virtual Return ToModel(Return model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.Number = Number;
            model.OrderId = OrderId;
            model.OrderNumber = OrderNumber;
            model.StoreId = StoreId;
            model.CustomerId = CustomerId;
            model.CustomerName = CustomerName;
            model.CustomerReference = CustomerReference;
            model.Status = Status;
            model.Resolution = Resolution;
            model.CustomerComment = CustomerComment;
            model.Comment = Comment;
            model.RejectReason = RejectReason;
            model.CancelReason = CancelReason;

            model.LineItems = LineItems.Select(x => x.ToModel(AbstractTypeFactory<ReturnLineItem>.TryCreateInstance())).ToList();

            return model;
        }

        public virtual ReturnEntity FromModel(Return model, PrimaryKeyResolvingMap pkMap)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            Number = model.Number;
            OrderId = model.OrderId;
            OrderNumber = model.OrderNumber;
            StoreId = model.StoreId;
            CustomerId = model.CustomerId;
            CustomerName = model.CustomerName;
            CustomerReference = model.CustomerReference;
            Status = model.Status;
            Resolution = model.Resolution;
            CustomerComment = model.CustomerComment;
            Comment = model.Comment;
            RejectReason = model.RejectReason;
            CancelReason = model.CancelReason;

            if (model.LineItems != null)
            {
                LineItems = new ObservableCollection<ReturnLineItemEntity>(model.LineItems
                    .Select(x => AbstractTypeFactory<ReturnLineItemEntity>.TryCreateInstance().FromModel(x, pkMap))
                    .OfType<ReturnLineItemEntity>());
            }

            return this;
        }

        public virtual void Patch(ReturnEntity target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.Number = Number;
            target.OrderId = OrderId;
            target.OrderNumber = OrderNumber;
            target.StoreId = StoreId;
            target.CustomerId = CustomerId;
            target.CustomerName = CustomerName;
            target.CustomerReference = CustomerReference;
            target.Status = Status;
            target.Resolution = Resolution;
            target.CustomerComment = CustomerComment;
            target.Comment = Comment;
            target.RejectReason = RejectReason;
            target.CancelReason = CancelReason;

            if (!LineItems.IsNullCollection())
            {
                LineItems.Patch(target.LineItems, (sourceItem, targetItem) => sourceItem.Patch(targetItem));
            }
        }
    }
}
