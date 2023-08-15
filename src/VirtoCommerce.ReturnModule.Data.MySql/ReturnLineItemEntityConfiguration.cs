using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtoCommerce.ReturnModule.Data.Models;

namespace VirtoCommerce.CartModule.Data.MySql
{
    public class ReturnLineItemEntityConfiguration : IEntityTypeConfiguration<ReturnLineItemEntity>
    {
        public void Configure(EntityTypeBuilder<ReturnLineItemEntity> builder)
        {
            builder.Property(x => x.Price).HasColumnType("decimal").HasPrecision(18, 4);
        }
    }
}
