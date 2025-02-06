using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Raido.Server.Internal;

namespace Raido.Server.Extensions
{ 
    /// <summary>
    /// Methods to add <see cref="IRaidoHubFilter"/>'s to RaidoHubs.
    /// </summary>
    public static class RaidoOptionsExtensions
    {
        /// <summary>
        /// Adds an instance of an <see cref="IRaidoHubFilter"/> to the <see cref="RaidoOptions"/>.
        /// </summary>
        /// <param name="options">The options to add a filter to.</param>
        /// <param name="hubFilter">The filter instance to add to the options.</param>
        public static void AddGlobalFilter(this RaidoOptions options, IRaidoHubFilter hubFilter)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            _ = hubFilter ?? throw new ArgumentNullException(nameof(hubFilter));

            if (options.GlobalHubFilters == null)
            {
                options.GlobalHubFilters = new List<IRaidoHubFilter>();
            }

            options.GlobalHubFilters.Add(hubFilter);
        }

        /// <summary>
        /// Adds an <see cref="IRaidoHubFilter"/> type to the <see cref="RaidoOptions"/> that will be resolved via DI or type activated.
        /// </summary>
        /// <typeparam name="TFilter">The <see cref="IRaidoHubFilter"/> type that will be added to the options.</typeparam>
        /// <param name="options">The options to add a filter to.</param>
        public static void AddGlobalFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TFilter>(this RaidoOptions options) where TFilter : IRaidoHubFilter
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));

            options.AddGlobalFilter(typeof(TFilter));
        }
        
        /// <summary>
        /// Adds an <see cref="IRaidoHubFilter"/> type to the <see cref="RaidoOptions"/> that will be resolved via DI or type activated.
        /// </summary>
        /// <param name="options">The options to add a filter to.</param>
        /// <param name="filterType">The <see cref="IRaidoHubFilter"/> type that will be added to the options.</param>
        public static void AddGlobalFilter(this RaidoOptions options, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type filterType)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            _ = filterType ?? throw new ArgumentNullException(nameof(filterType));

            options.AddGlobalFilter(new RaidoHubFilterFactory(filterType));
        }
    }
}