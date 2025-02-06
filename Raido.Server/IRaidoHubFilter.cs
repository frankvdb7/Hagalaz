using System;
using System.Threading.Tasks;

namespace Raido.Server
{
    /// <summary>
    /// The filter abstraction for hub method invocations.
    /// </summary>
    public interface IRaidoHubFilter
    {
        /// <summary>
        /// Allows handling of all Hub method invocations.
        /// </summary>
        /// <param name="invocationContext">The context for the method invocation that holds all the important information about the invoke.</param>
        /// <param name="next">The next filter to run, and for the final one, the Hub invocation.</param>
        /// <returns>Returns the result of the Hub method invoke.</returns>
        ValueTask<object?> InvokeMethodAsync(RaidoHubInvocationContext invocationContext, Func<RaidoHubInvocationContext, ValueTask<object?>> next) => next(invocationContext);

        /// <summary>
        /// Allows handling of the <see cref="RaidoHub.OnConnectedAsync"/> method.
        /// </summary>
        /// <param name="context">The context for OnConnectedAsync.</param>
        /// <param name="next">The next filter to run, and for the final one, the Hub invocation.</param>
        /// <returns></returns>
        Task OnConnectedAsync(RaidoHubLifetimeContext context, Func<RaidoHubLifetimeContext, Task> next) => next(context);

        /// <summary>
        /// Allows handling of the <see cref="RaidoHub.OnDisconnectedAsync(Exception)"/> method.
        /// </summary>
        /// <param name="context">The context for OnDisconnectedAsync.</param>
        /// <param name="exception">The exception, if any, for the connection closing.</param>
        /// <param name="next">The next filter to run, and for the final one, the Hub invocation.</param>
        /// <returns></returns>
        Task OnDisconnectedAsync(RaidoHubLifetimeContext context, Exception? exception, Func<RaidoHubLifetimeContext, Exception?, Task> next) => next(context, exception);
    }
}