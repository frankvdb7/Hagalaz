using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Events.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    ///     Represents a single creature inside the game world, such as a player or an NPC.
    /// </summary>
    public interface ICreature : IEntity
    {
        /// <summary>
        /// Gets or sets the unique server-side index of the creature.
        /// </summary>
        int Index { get; set; }
        /// <summary>
        /// Gets the name of the creature as it should be displayed to players.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// Gets the special movement, such as a knockback or jump, that is currently being rendered for this creature.
        /// </summary>
        IForceMovement? RenderedNonstandardMovement { get; }
        /// <summary>
        /// Gets the creature that this creature is currently facing.
        /// </summary>
        ICreature? FacedCreature { get; }
        /// <summary>
        /// Gets the creature's location from the previous game tick.
        /// </summary>
        ILocation? LastLocation { get; }
        /// <summary>
        /// Gets the creature's viewport, which manages the set of other entities visible to it.
        /// </summary>
        IViewport Viewport { get; }
        /// <summary>
        /// Gets the combat handler for this creature, which manages its combat state and actions.
        /// </summary>
        ICreatureCombat Combat { get; }
        /// <summary>
        /// Gets the current map area the creature is in.
        /// </summary>
        IArea Area { get; }
        /// <summary>
        /// Gets the movement handler for this creature, which manages its pathfinding and position updates.
        /// </summary>
        IMovement Movement { get; }
        /// <summary>
        /// Gets the service scope for dependency injection.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
        /// <summary>
        /// Gets the mediator for this creature, used for handling game events and commands in a decoupled manner.
        /// </summary>
        IGameMediator Mediator { get; }
        /// <summary>
        /// Gets the X-coordinate that the creature is currently facing. A value of -1 indicates no specific location is being faced.
        /// </summary>
        int TurnedToX { get; }
        /// <summary>
        /// Gets the Y-coordinate that the creature is currently facing. A value of -1 indicates no specific location is being faced.
        /// </summary>
        int TurnedToY { get; }
        /// <summary>
        /// Gets the text that the creature is speaking during the current game tick, or null if it is not speaking.
        /// </summary>
        string? SpeakingText { get; }
        /// <summary>
        /// Gets the graphical glow effect currently rendered on the creature.
        /// </summary>
        IGlow? RenderedGlow { get; }
        /// <summary>
        /// Gets a read-only list of the hitsplats queued to be displayed on the creature in the current tick.
        /// </summary>
        IReadOnlyList<IHitSplat> RenderedHitSplats { get; }
        /// <summary>
        /// Gets a read-only list of the hit bars queued to be displayed on the creature in the current tick.
        /// </summary>
        IReadOnlyList<IHitBar> RenderedHitBars { get; }
        /// <summary>
        /// Gets an enumeration of all active states on the creature.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IState"/> currently affecting the creature.</returns>
        public IEnumerable<IState> GetStates();
        /// <summary>
        /// Gets the cardinal or intercardinal direction the creature is currently facing.
        /// </summary>
        DirectionFlag FaceDirection { get; }
        /// <summary>
        /// Gets the pathfinding implementation used by this creature.
        /// </summary>
        IPathFinder PathFinder { get; }
        /// <summary>
        /// Queues a special movement (e.g., knockback, jump) to be rendered for this creature.
        /// </summary>
        /// <param name="movement">The force movement to be rendered.</param>
        void QueueForceMovement(IForceMovement movement);
        /// <summary>
        /// Determines whether this creature is within a specified distance of another creature.
        /// </summary>
        /// <param name="otherCreature">The creature to check the distance to.</param>
        /// <param name="range">The maximum allowed distance.</param>
        /// <returns><c>true</c> if the distance between the creatures is within the specified range; otherwise, <c>false</c>.</returns>
        bool WithinRange(ICreature otherCreature, int range);
        /// <summary>
        /// Determines whether this creature is within a specified distance of a given location, taking the target's size into account.
        /// </summary>
        /// <param name="otherLocation">The location to check the distance to.</param>
        /// <param name="otherSize">The size of the target at the location.</param>
        /// <param name="range">The maximum allowed distance.</param>
        /// <returns><c>true</c> if the distance is within the specified range; otherwise, <c>false</c>.</returns>
        bool WithinRange(ILocation otherLocation, int otherSize, int range);
        /// <summary>
        /// A callback method that is executed when this creature is killed by another creature.
        /// </summary>
        /// <param name="killer">The creature that delivered the killing blow.</param>
        void OnKilledBy(ICreature killer);
        /// <summary>
        /// Resets the creature's facing direction to its default (typically South).
        /// </summary>
        void ResetFacing();
        /// <summary>
        /// Makes this creature face another creature.
        /// </summary>
        /// <param name="creature">The creature to face.</param>
        void FaceCreature(ICreature creature);
        /// <summary>
        /// Determines if this creature should be visible to a specific player character.
        /// </summary>
        /// <param name="viewer">The character viewing this creature.</param>
        /// <returns><c>true</c> if this creature should be rendered for the viewer; otherwise, <c>false</c>.</returns>
        bool ShouldBeRenderedFor(ICharacter viewer);
        /// <summary>
        /// Determines if this creature should be visible to a specific NPC.
        /// </summary>
        /// <param name="viewer">The NPC viewing this creature.</param>
        /// <returns><c>true</c> if this creature should be rendered for the viewer; otherwise, <c>false</c>.</returns>
        bool ShouldBeRenderedFor(INpc viewer);
        /// <summary>
        /// Resets the creature's stats and teleports it to its spawn location. This also calls the OnSpawn method.
        /// </summary>
        void Respawn();
        /// <summary>
        /// A callback method that is executed when the creature's health reaches zero.
        /// </summary>
        void OnDeath();
        /// <summary>
        /// Checks if the creature currently has a specific state active.
        /// </summary>
        /// <typeparam name="T">The type of the state to check for.</typeparam>
        /// <returns><c>true</c> if a state of the specified type is active; otherwise, <c>false</c>.</returns>
        bool HasState<T>() where T : IState;
        /// <summary>
        /// Performs the main update logic for the creature's client-side representation, such as movement and animation.
        /// </summary>
        Task MajorClientUpdateTickAsync();
        /// <summary>
        /// Prepares the data required for the client update tick, gathering all necessary information before the main update.
        /// </summary>
        Task MajorClientPrepareUpdateTickAsync();
        /// <summary>
        /// Resets the creature's update flags and clears temporary rendering data after a client update has been sent.
        /// </summary>
        Task MajorClientUpdateResetTickAsync();
        /// <summary>
        /// Performs the main game logic update for the creature, such as processing states, timers, and combat actions.
        /// </summary>
        void MajorUpdateTick();
        /// <summary>
        /// Interrupts the creature's current action (e.g., skilling, combat) due to an external event.
        /// </summary>
        /// <param name="source">The object or system that initiated the interruption.</param>
        void Interrupt(object source);
        /// <summary>
        /// A callback method that is executed when this creature successfully kills another creature.
        /// </summary>
        /// <param name="target">The creature that was killed.</param>
        void OnTargetKilled(ICreature target);
        /// <summary>
        /// Adds a state to the creature. If a state of the same type already exists, the one with the longer duration is kept.
        /// </summary>
        /// <param name="state">The state to add.</param>
        void AddState(IState state);
        /// <summary>
        /// Removes a state of a specific type from the creature.
        /// </summary>
        /// <typeparam name="T">The type of the state to remove.</typeparam>
        void RemoveState<T>() where T : IState;
        /// <summary>
        /// Makes the creature display a line of text above its head.
        /// </summary>
        /// <param name="text">The text to be spoken.</param>
        void Speak(string text);
        /// <summary>
        /// Stuns the creature for a specified number of game ticks, preventing most actions.
        /// </summary>
        /// <param name="ticks">The duration of the stun in ticks.</param>
        void Stun(int ticks);
        /// <summary>
        /// Freezes the creature for a specified number of game ticks, preventing movement. Also applies a period of immunity to subsequent freezes.
        /// </summary>
        /// <param name="ticks">The duration of the freeze in ticks.</param>
        /// <param name="immunityTicks">The duration of freeze immunity in ticks after the freeze ends.</param>
        /// <returns><c>true</c> if the creature was successfully frozen; otherwise, <c>false</c>.</returns>
        bool Freeze(int ticks, int immunityTicks);
        /// <summary>
        /// Applies poison to the creature with a given strength. A strength less than 10 will cure the poison.
        /// </summary>
        /// <param name="amount">The strength of the poison to apply.</param>
        /// <returns><c>true</c> if the creature was successfully poisoned; otherwise, <c>false</c>.</returns>
        bool Poison(short amount);
        /// <summary>
        /// Queues a graphical effect to be displayed on the creature.
        /// </summary>
        /// <param name="graphic">The graphic to display.</param>
        void QueueGraphic(IGraphic graphic);
        /// <summary>
        /// Queues an animation to be played by the creature.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        void QueueAnimation(IAnimation animation);
        /// <summary>
        /// Queues a hitsplat to be displayed on the creature.
        /// </summary>
        /// <param name="splat">The hitsplat to display.</param>
        void QueueHitSplat(IHitSplat splat);
        /// <summary>
        /// Queues a hit bar update to be displayed for the creature.
        /// </summary>
        /// <param name="hitBar">The hit bar to display.</param>
        void QueueHitBar(IHitBar hitBar);
        /// <summary>
        /// Queues a background task to be executed in the context of this creature.
        /// </summary>
        /// <param name="task">The task to be queued.</param>
        /// <returns>A handle to the queued task.</returns>
        IRsTaskHandle QueueTask(ITaskItem task);
        /// <summary>
        /// Queues a background task with a return value to be executed in the context of this creature.
        /// </summary>
        /// <typeparam name="TResult">The type of the task's result.</typeparam>
        /// <param name="task">The task to be queued.</param>
        /// <returns>A handle to the queued task, which can be used to retrieve the result.</returns>
        IRsTaskHandle<TResult> QueueTask<TResult>(ITaskItem<TResult> task);
        /// <summary>
        /// Makes the creature face a specific location. The location must be within the game map bounds.
        /// </summary>
        /// <param name="location">The location to face.</param>
        /// <param name="tileSizeX">The horizontal size of the target area to face. Must be at least 1.</param>
        /// <param name="tileSizeY">The vertical size of the target area to face. Must be at least 1.</param>
        void FaceLocation(ILocation location, int tileSizeX = 1, int tileSizeY = 1);
        /// <summary>
        /// Queues a glow effect to be displayed on the creature.
        /// </summary>
        /// <param name="glow">The glow effect to display.</param>
        void QueueGlow(IGlow glow);
        /// <summary>
        /// Checks if there are any registered event handlers for a specific type of creature event.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event to check for.</typeparam>
        /// <returns><c>true</c> if at least one handler is registered for the event type; otherwise, <c>false</c>.</returns>
        bool HasEventHandler<TEventType>() where TEventType : ICreatureEvent;
        /// <summary>
        /// Registers a callback to be executed when a specific type of creature event occurs.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">The callback delegate to execute.</param>
        /// <returns>An object representing the registered event handler, which can be used for unregistering.</returns>
        EventHappened RegisterEventHandler<TEventType>(EventHappened<TEventType> handler) where TEventType : ICreatureEvent;
        /// <summary>
        /// Unregisters a previously registered event handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">The handler instance to unregister.</param>
        /// <returns><c>true</c> if the handler was successfully unregistered; otherwise, <c>false</c>.</returns>
        bool UnregisterEventHandler<TEventType>(EventHappened handler) where TEventType : ICreatureEvent;
        /// <summary>
        /// A callback method that is executed when the creature is successfully registered and added to the game world.
        /// </summary>
        Task OnRegistered();
    }
}
