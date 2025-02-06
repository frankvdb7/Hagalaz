using System;

namespace Raido.Server
{
    public sealed class RaidoHubLifetimeContext
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="RaidoHubLifetimeContext"/> class.
        /// </summary>
        /// <param name="context">Context for the active Hub connection and caller.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> specific to the scope of this Hub method invocation.</param>
        /// <param name="hub">The instance of the Hub.</param>
        public RaidoHubLifetimeContext(RaidoCallerContext context, IServiceProvider serviceProvider, RaidoHub hub)
        {
            Hub = hub;
            ServiceProvider = serviceProvider;
            Context = context;
        }

        /// <summary>
        /// Gets the context for the active Hub connection and caller.
        /// </summary>
        public RaidoCallerContext Context { get; }

        /// <summary>
        /// Gets the Hub instance.
        /// </summary>
        public RaidoHub Hub { get; }

        /// <summary>
        /// The <see cref="IServiceProvider"/> specific to the scope of this Hub method invocation.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }
    }
}