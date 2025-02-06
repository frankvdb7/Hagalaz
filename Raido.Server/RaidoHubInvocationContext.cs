using System;
using System.Collections.Generic;
using System.Reflection;
using Raido.Server.Internal.Reflection;

namespace Raido.Server
{
    public class RaidoHubInvocationContext
    {
        internal ObjectMethodExecutor ObjectMethodExecutor { get; }

        /// <summary>
        /// Gets the context for the active Hub connection and caller.
        /// </summary>
        public RaidoCallerContext Context { get; }

        /// <summary>
        /// Gets the Hub instance.
        /// </summary>
        public RaidoHub Hub { get; }

        /// <summary>
        /// Gets the arguments provided by the client.
        /// </summary>
        public IReadOnlyList<object?> HubMethodArguments { get; }

        /// <summary>
        /// The <see cref="MethodInfo"/> for the Hub method being invoked.
        /// </summary>
        public MethodInfo HubMethod { get; }

        /// <summary>
        /// The <see cref="IServiceProvider"/> specific to the scope of this Hub method invocation.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Instantiates a new instance of the <see cref="RaidoHubInvocationContext"/> class.
        /// </summary>
        /// <param name="objectMethodExecutor"></param>
        /// <param name="context"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="hub"></param>
        /// <param name="hubMethod"></param>
        /// <param name="hubMethodArguments"></param>
        internal RaidoHubInvocationContext(
            ObjectMethodExecutor objectMethodExecutor,
            RaidoCallerContext context,
            IServiceProvider serviceProvider,
            RaidoHub hub,
            IReadOnlyList<object?> hubMethodArguments)
        {
            ObjectMethodExecutor = objectMethodExecutor;
            Context = context;
            ServiceProvider = serviceProvider;
            Hub = hub;
            HubMethod = objectMethodExecutor.MethodInfo;
            HubMethodArguments = hubMethodArguments;
        }
    }
}