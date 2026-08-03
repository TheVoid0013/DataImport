using DataImport.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace DataImport.API.Configuration
{
    public static class DatabaseConfiguration
    {
        /// <summary>
        /// Configures and adds the application's database context to the service collection using SQL Server.
        /// It sets up the connection string from configuration, enables retry on failure with a maximum of 5 retries,
        /// sets a max retry delay of 10 seconds, and configures the command timeout to 60 seconds.
        /// </summary>
        public static IServiceCollection AddDatabaseConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SanctionsDb");

            services.AddDbContext<SanctionsDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sqlOptions => sqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null)
                        .CommandTimeout(60)
                ));

            return services;
        }


    }
}
