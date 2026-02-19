using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Repository
{
    public class ScanWorkerContextFactory : IDesignTimeDbContextFactory<ScanWorkerContext>
    {
        public ScanWorkerContext CreateDbContext(string[] args)
        {
            // Build config manually since Program.cs DI is not available
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ScanWorkerContext>();
            var connectionString = config.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new ScanWorkerContext(optionsBuilder.Options);
        }
    }
}