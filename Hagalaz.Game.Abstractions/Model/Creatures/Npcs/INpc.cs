using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    public interface INpc : ICreature
    {
        /// <summary>
        /// Gets the appearance.
        /// </summary>
        /// <value>
        /// The appearance.
        /// </value>
        INpcAppearance Appearance { get; }

        /// <summary>
        /// Gets the definition.
        /// </summary>
        /// <value>
        /// The definition.
        /// </value>
        INpcDefinition Definition { get; }

        /// <summary>
        /// Gets the render information.
        /// </summary>
        /// <value>
        /// The render information.
        /// </value>
        INpcRenderInformation RenderInformation { get; }

        /// <summary>
        /// Gets the statistics.
        /// </summary>
        /// <value>
        /// The statistics.
        /// </value>
        INpcStatistics Statistics { get; }

        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <value>
        /// The script.
        /// </value>
        INpcScript Script { get; }

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        IBounds Bounds { get; }

        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the cript type.</typeparam>
        /// <returns></returns>
        TScriptType? GetScript<TScriptType>() where TScriptType : class, INpcScript;

        /// <summary>
        /// Determines whether this instance has script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the cript type.</typeparam>
        /// <returns></returns>
        bool HasScript<TScriptType>() where TScriptType : class, INpcScript;

        /// <summary>
        /// Tries to get the npc script of the supplied type.
        /// Returns true if script is found.
        /// </summary>
        /// <typeparam name="TScriptType"></typeparam>
        /// <returns></returns>
        bool TryGetScript<TScriptType>([NotNullWhen(true)] out TScriptType? script) where TScriptType : class, INpcScript;
    }
}