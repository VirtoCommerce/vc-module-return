using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;

namespace VirtoCommerce.Return.Data.Repositories
{
    public class ReturnDbContext : DbContextWithTriggers
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
            //        modelBuilder.Entity<ReturnEntity>().ToTable("MyModule").HasKey(x => x.Id);
            //        modelBuilder.Entity<ReturnEntity>().Property(x => x.Id).HasMaxLength(128);
            //        base.OnModelCreating(modelBuilder);
        }
    }
}

