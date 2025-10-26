using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shared
{
    public class SaaSMvpContextFactory : IDesignTimeDbContextFactory<SaaSMvpContext>
    {
        public SaaSMvpContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SaaSMvpContext>();
            optionsBuilder.UseSqlServer(
                 //"Server=DESKTOP-SH6IB0S;Database=SaaSMvp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
                 "Server=HP01;Database=SaaSMvp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
            );

            return new SaaSMvpContext(optionsBuilder.Options);
        }
    }
}