using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hagalaz.Data
{
    internal class HagalazDbContextFactory : IDesignTimeDbContextFactory<HagalazDbContext>
    {
        public HagalazDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<HagalazDbContext>()
                .UseMySql(MySqlServerVersion.LatestSupportedServerVersion,
                    options => {
#pragma warning disable CS0618
            options.TranslateParameterizedCollectionsToConstants();
#pragma warning restore CS0618
                    })
                .UseOpenIddict()
                .Options;
            return new HagalazDbContext(options);
        }
    }
}
