using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        [Column(TypeName = "Money")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        [StringLength(1024)]
        public string Reason { get; set; }

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
            model.Price = Price;
            model.Quantity = Quantity;
            model.Reason = Reason;

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
            Price = model.Price;
            Quantity = model.Quantity;
            Reason = model.Reason;

            return this;
        }

        public void Patch(ReturnLineItemEntity target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.ReturnId = ReturnId;
            target.OrderLineItemId = OrderLineItemId;
            target.Price = Price;
            target.Quantity = Quantity;
            target.Reason = Reason;
        }
    }
}
