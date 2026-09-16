using System.Reflection;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.ReturnModule.Data.Models;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.ReturnModule.Data.Repositories
{
    public class ReturnDbContext : DbContextBase
    {
        public ReturnDbContext(DbContextOptions<ReturnDbContext> options)
          : base(options)
        {
        }

        protected ReturnDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Return

            modelBuilder.Entity<ReturnEntity>().ToTable("Return").HasKey(x => x.Id);
            modelBuilder.Entity<ReturnEntity>().Property(x => x.Id).HasMaxLength(IdLength).ValueGeneratedOnAdd();

            // "My returns" always filters by customer and store and sorts by date, so the composite
            // serves the seek and the order in one pass; every order page asks what the order is holding.
            modelBuilder.Entity<ReturnEntity>().HasIndex(x => new { x.CustomerId, x.StoreId, x.CreatedDate });
            modelBuilder.Entity<ReturnEntity>().HasIndex(x => x.OrderId);

            #endregion Return

            #region ReturnLineItem

            modelBuilder.Entity<ReturnLineItemEntity>().ToTable("ReturnLineItem").HasKey(x => x.Id);
            modelBuilder.Entity<ReturnLineItemEntity>().Property(x => x.Id).HasMaxLength(IdLength).ValueGeneratedOnAdd();
            modelBuilder.Entity<ReturnLineItemEntity>().HasOne(x => x.Return).WithMany(x => x.LineItems)
                .HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);

            #endregion ReturnLineItem

            #region ReturnAttachment

            modelBuilder.Entity<ReturnAttachmentEntity>().ToTable("ReturnAttachment").HasKey(x => x.Id);
            modelBuilder.Entity<ReturnAttachmentEntity>().Property(x => x.Id).HasMaxLength(IdLength).ValueGeneratedOnAdd();
            modelBuilder.Entity<ReturnAttachmentEntity>().HasOne(x => x.ReturnLineItem).WithMany(x => x.Attachments)
                .HasForeignKey(x => x.ReturnLineItemId).OnDelete(DeleteBehavior.Cascade);

            #endregion ReturnAttachment

            base.OnModelCreating(modelBuilder);

            switch (this.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ReturnModule.Data.MySql"));
                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ReturnModule.Data.PostgreSql"));
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ReturnModule.Data.SqlServer"));
                    break;
            }
        }
    }
}
