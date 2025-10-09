using System;
using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Scripts;

namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an NPC where optional
    /// parameters like movement bounds, scripts, and facing direction can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="INpcBuilder"/>.
    /// It also inherits from <see cref="INpcBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INpcOptional : INpcBuild
    {
        /// <summary>
        /// Sets the minimum boundary for the NPC's movement area.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> representing the minimum corner of the bounding box.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        INpcOptional WithMinimumBounds(ILocation location);

        /// <summary>
        /// Sets the maximum boundary for the NPC's movement area.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> representing the maximum corner of the bounding box.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        INpcOptional WithMaximumBounds(ILocation location);

        /// <summary>
        /// Attaches a specific script instance to the NPC to define its behavior.
        /// </summary>
        /// <param name="script">An instance of a class that implements <see cref="INpcScript"/>.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        INpcOptional WithScript(INpcScript script);

        /// <summary>
        /// Attaches a script to the NPC by its type. The script will be resolved from the dependency injection container.
        /// </summary>
        /// <typeparam name="TScript">The type of the script, which must implement <see cref="INpcScript"/>.</typeparam>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        INpcOptional WithScript<TScript>() where TScript : INpcScript;

        /// <summary>
        /// Attaches a script to the NPC by its <see cref="Type"/>. The script will be resolved from the dependency injection container.
        /// </summary>
        /// <param name="type">The type of the script, which must implement <see cref="INpcScript"/>.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        INpcOptional WithScript(Type type);

        /// <summary>
        /// Sets the initial direction the NPC should face upon spawning.
        /// </summary>
        /// <param name="direction">The <see cref="DirectionFlag"/> indicating the direction to face.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        INpcOptional WithFaceDirection(DirectionFlag direction);
    }
}