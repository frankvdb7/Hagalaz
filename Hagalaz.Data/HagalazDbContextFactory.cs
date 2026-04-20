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
                    options => { options.UseParameterizedCollectionMode(ParameterTranslationMode.Constant); })
                .UseOpenIddict()
                .Options;
            return new HagalazDbContext(options);
        }
    }
}
