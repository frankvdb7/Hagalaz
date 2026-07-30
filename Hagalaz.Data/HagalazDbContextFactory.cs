using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hagalaz.Data
{
    internal class HagalazDbContextFactory : IDesignTimeDbContextFactory<HagalazDbContext>
    {
        public HagalazDbContext CreateDbContext(string[] args)
        {
            var connectionString = args
                .FirstOrDefault(static argument => argument.StartsWith("--connection=", StringComparison.OrdinalIgnoreCase))?
                .Split('=', 2)[1]
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__hagalaz-db");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "A design-time MySQL connection string is required. Set ConnectionStrings__hagalaz-db or pass --connection=<connection-string>.");
            }

            connectionString = MySqlConnectionStringCompatibility.NormalizeForOracle(connectionString, "design-time");

            var options = new DbContextOptionsBuilder<HagalazDbContext>()
                .UseMySQL(connectionString, options => options.EnableRetryOnFailure(6))
                .UseOpenIddict()
                .Options;
            return new HagalazDbContext(options);
        }
    }
}
