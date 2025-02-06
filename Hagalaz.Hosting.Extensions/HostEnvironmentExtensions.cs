using System;
using Microsoft.Extensions.Hosting;

namespace Hagalaz.Hosting.Extensions
{
    public static class HostEnvironmentExtensions
    {
        /// <summary>
        /// Determines whether [is docker container].
        /// </summary>
        /// <param name="env">The env.</param>
        /// <returns>
        ///   <c>true</c> if [is docker container] [the specified env]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDockerContainer(this IHostEnvironment env) => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }
}
