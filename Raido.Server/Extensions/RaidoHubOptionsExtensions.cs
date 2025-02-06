using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Raido.Server.Internal;

namespace Raido.Server.Extensions
{ 
    /// <summary>
    /// Methods to add <see cref="IRaidoHubFilter"/>'s to RaidoHubs.
    /// </summary>
    public static class RaidoHubOptionsExtensions
    {
        /// <summary>
        /// Adds an instance of an <see cref="IRaidoHubFilter"/> to the <see cref="RaidoOptions"/>.
        /// </summary>
        /// <param name="options">The options to add a filter to.</param>
        /// <param name="hubFilter">The filter instance to add to the options.</param>
        public static void AddFilter<THub>(this RaidoHubOptions<THub> options, IRaidoHubFilter hubFilter) 
            where THub : RaidoHub
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            _ = hubFilter ?? throw new ArgumentNullException(nameof(hubFilter));

            if (options.HubFilters == null)
            {
                options.HubFilters = new List<IRaidoHubFilter>();
            }

            options.HubFilters.Add(hubFilter);
        }

        /// <summary>
        /// Adds an <see cref="IRaidoHubFilter"/> type to the <see cref="RaidoOptions"/> that will be resolved via DI or type activated.
        /// </summary>
        /// <typeparam name="TFilter">The <see cref="IRaidoHubFilter"/> type that will be added to the options.</typeparam>
        /// <param name="options">The options to add a filter to.</param>
        public static void AddFilter<THub, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TFilter>(this RaidoHubOptions<THub> options) 
            where TFilter : IRaidoHubFilter 
            where THub : RaidoHub
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));

            options.AddFilter(typeof(TFilter));
        }
        
        /// <summary>
        /// Adds an <see cref="IRaidoHubFilter"/> type to the <see cref="RaidoOptions"/> that will be resolved via DI or type activated.
        /// </summary>
        /// <param name="options">The options to add a filter to.</param>
        /// <param name="filterType">The <see cref="IRaidoHubFilter"/> type that will be added to the options.</param>
        public static void AddFilter<THub>(this RaidoHubOptions<THub> options, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type filterType)
            where THub : RaidoHub
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            _ = filterType ?? throw new ArgumentNullException(nameof(filterType));

            options.AddFilter(new RaidoHubFilterFactory(filterType));
        }
    }
}