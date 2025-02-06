using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Hagalaz.Hosting.Extensions
{
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Uses the assembly console title.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IHostBuilder UseAssemblyConsoleTitle(this IHostBuilder builder)
        {
            var assembly = Assembly.GetCallingAssembly();
            Console.Title = assembly?.GetName()?.Name ?? string.Empty;
            return builder;
        }
    }
}
