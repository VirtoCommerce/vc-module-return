using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Data.Models
{
    public class ReturnEntity : AuditableEntity, IDataEntity<ReturnEntity, Return>
    {
        [Required]
        [StringLength(64)]
        public string Number { get; set; }

        [Required]
        [StringLength(128)]
        public string OrderId { get; set; }

        [StringLength(64)]
        public string Status { get; set; }

        [StringLength(2048)]
        public string Resolution { get; set; }

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
            model.Status = Status;
            model.Resolution = Resolution;

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
            Status = model.Status;
            Resolution = model.Resolution;

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
            target.Status = Status;
            target.Resolution = Resolution;

            if (!LineItems.IsNullCollection())
            {
                LineItems.Patch(target.LineItems, (sourceItem, targetItem) => sourceItem.Patch(targetItem));
            }
        }
    }
}
