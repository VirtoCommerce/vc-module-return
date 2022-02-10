using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VirtoCommerce.Return.Data.Repositories
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReturnDbContext>
    {
        public ReturnDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ReturnDbContext>();

            builder.UseSqlServer("Data Source=(local);Initial Catalog=VirtoCommerce3;Persist Security Info=True;User ID=virto;Password=virto;MultipleActiveResultSets=True;Connect Timeout=30");

            return new ReturnDbContext(builder.Options);
        }
    }
}
