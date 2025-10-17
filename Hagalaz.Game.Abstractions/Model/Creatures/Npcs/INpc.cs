using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for a non-player character (NPC), extending the base creature with NPC-specific properties and behaviors.
    /// </summary>
    public interface INpc : ICreature
    {
        /// <summary>
        /// Gets the handler for the NPC's visual appearance.
        /// </summary>
        INpcAppearance Appearance { get; }

        /// <summary>
        /// Gets the data definition for this NPC, containing its base stats and properties.
        /// </summary>
        INpcDefinition Definition { get; }

        /// <summary>
        /// Gets the handler for the NPC's client-side rendering information.
        /// </summary>
        INpcRenderInformation RenderInformation { get; }

        /// <summary>
        /// Gets the handler for the NPC's statistics.
        /// </summary>
        INpcStatistics Statistics { get; }

        /// <summary>
        /// Gets the script that controls the NPC's behavior and logic.
        /// </summary>
        INpcScript Script { get; }

        /// <summary>
        /// Gets the movement boundaries for the NPC.
        /// </summary>
        IBounds Bounds { get; }

        /// <summary>
        /// Retrieves a script of a specific type that is attached to the NPC.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to retrieve.</typeparam>
        /// <returns>The script instance if found; otherwise, <c>null</c>.</returns>
        TScriptType? GetScript<TScriptType>() where TScriptType : class, INpcScript;

        /// <summary>
        /// Checks if the NPC has a script of a specific type attached.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to check for.</typeparam>
        /// <returns><c>true</c> if a script of the specified type is attached; otherwise, <c>false</c>.</returns>
        bool HasScript<TScriptType>() where TScriptType : class, INpcScript;

        /// <summary>
        /// Tries to get a script of a specific type that is attached to the NPC.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to retrieve.</typeparam>
        /// <param name="script">When this method returns, contains the script instance if found; otherwise, null.</param>
        /// <returns><c>true</c> if a script of the specified type was found; otherwise, <c>false</c>.</returns>
        bool TryGetScript<TScriptType>([NotNullWhen(true)] out TScriptType? script) where TScriptType : class, INpcScript;
    }
}