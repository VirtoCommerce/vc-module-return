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
        public string ReturnNumber { get; set; }

        public string OrderId { get; set; }

        public string ReturnStatus { get; set; }

        public virtual ObservableCollection<ReturnLineItemEntity> ReturnLineItems { get; set; } = new NullCollection<ReturnLineItemEntity>();

        public virtual Return ToModel(Return model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.ReturnNumber = ReturnNumber;
            model.OrderId = OrderId;
            model.ReturnStatus = ReturnStatus;

            model.ReturnLineItems = ReturnLineItems.Select(x => x.ToModel(AbstractTypeFactory<ReturnLineItem>.TryCreateInstance())).ToList();

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

            ReturnNumber = model.ReturnNumber;
            OrderId = model.OrderId;
            ReturnStatus = model.ReturnStatus;

            if (model.ReturnLineItems != null)
            {
                ReturnLineItems = new ObservableCollection<ReturnLineItemEntity>(model.ReturnLineItems
                    .Select(x => AbstractTypeFactory<ReturnLineItemEntity>.TryCreateInstance().FromModel(x, pkMap))
                    .OfType<ReturnLineItemEntity>());
            }

            return this;
        }

        public virtual void Patch(ReturnEntity target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.ReturnNumber = ReturnNumber;
            target.OrderId = OrderId;
            target.ReturnStatus = ReturnStatus;

            if (!ReturnLineItems.IsNullCollection())
            {
                ReturnLineItems.Patch(target.ReturnLineItems, (sourceItem, targetItem) => sourceItem.Patch(targetItem));
            }
        }
    }
}
