using System;
using System.ComponentModel.DataAnnotations;
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

            return this;
        }

        public void Patch(ReturnLineItemEntity target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.ReturnId = ReturnId;
            target.OrderLineItemId = OrderLineItemId;
        }
    }
}
