using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DataImport.Data.Data
{
    public class SanctionsDbContextFactory : IDesignTimeDbContextFactory<SanctionsDbContext>
    {
        public SanctionsDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
            var connectionString = configuration.GetConnectionString("SanctionsDb");

            var optionsBuilder = new DbContextOptionsBuilder<SanctionsDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SanctionsDbContext(optionsBuilder.Options);
        }
    }
}
