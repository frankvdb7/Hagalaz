using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Hagalaz.Services.Extensions
{
    /// <summary>
    /// Provides extension methods to perform async initialization of an application.
    /// </summary>
    public static class HostExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHostBuilder UseGlobalExceptionHandling(this IHostBuilder builder)
        {
            return builder.ConfigureServices((context, services) =>
            {
                services.TryAddSingleton<IGlobalExceptionService, GlobalExceptionService>();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IHost RunGlobalExceptionHandling(this IHost host)
        {
            host.Services.GetRequiredService<IGlobalExceptionService>(); // startup
            return host;
        }
    }
}
