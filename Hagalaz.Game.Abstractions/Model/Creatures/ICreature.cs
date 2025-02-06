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
    ///     Represents a single creature inside the game world.
    /// </summary>
    public interface ICreature : IEntity
    {
        /// <summary>
        /// Gets a non-zero based index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        int Index { get; set; }
        /// <summary>
        /// Contains the display name of the creature.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// Gets the rendered nonstandard movement.
        /// </summary>
        /// <value>
        /// The rendered nonstandard movement.
        /// </value>
        IForceMovement? RenderedNonstandardMovement { get; }
        /// <summary>
        /// Gets the faced creature.
        /// </summary>
        /// <value>
        /// The faced creature.
        /// </value>
        ICreature? FacedCreature { get; }
        /// <summary>
        /// Gets the last location.
        /// </summary>
        /// <value>
        /// The last location.
        /// </value>
        ILocation? LastLocation { get; }
        /// <summary>
        /// Gets the game map.
        /// </summary>
        /// <value>
        /// The game map.
        /// </value>
        IViewport Viewport { get; }
        /// <summary>
        /// Gets the combat.
        /// </summary>
        /// <value>
        /// The combat.
        /// </value>
        ICreatureCombat Combat { get; }
        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <value>
        /// The area.
        /// </value>
        IArea Area { get; }
        /// <summary>
        /// Gets the movement.
        /// </summary>
        /// <value>
        /// The movement.
        /// </value>
        IMovement Movement { get; }
        /// <summary>
        /// Gets the service scope for dependency injection.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
        /// <summary>
        /// Gets the mediator for this creature
        /// </summary>
        IGameMediator Mediator { get; }
        /// <summary>
        ///     Contains faced X coordinate.
        ///     -1 if no specific location is faced.
        /// </summary>
        /// <value>The turned to X.</value>
        int TurnedToX { get; }
        /// <summary>
        ///     Contains faced Y coordinate.
        ///     -1 if no specific location is faced.
        /// </summary>
        /// <value>The turned to Y.</value>
        int TurnedToY { get; }
        /// <summary>
        ///     Contains creature speaking text or null if it isn't speaking
        ///     anything this tick.
        /// </summary>
        /// <value>The speaking text.</value>
        string? SpeakingText { get; }
        /// <summary>
        ///     Contains rendered glow.
        /// </summary>
        /// <value>The rendered glow.</value>
        IGlow? RenderedGlow { get; }
        /// <summary>
        ///     Contains rendered hitsplats.
        /// </summary>
        /// <value>The rendered hit splats.</value>
        IReadOnlyList<IHitSplat> RenderedHitSplats { get; }
        /// <summary>
        ///     Contains  the rendered hit bars.
        /// </summary>
        /// <value>
        ///     The rendered hit bars.
        /// </value>
        IReadOnlyList<IHitBar> RenderedHitBars { get; }
        public IEnumerable<IState> GetStates();
        /// <summary>
        ///     Get's facing direction of this creature.
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        DirectionFlag FaceDirection { get; }
        IPathFinder PathFinder { get; }
        /// <summary>
        ///     Draw's nonstandart movement.
        /// </summary>
        /// <param name="movement">Movement which should be rendered.</param>
        void QueueForceMovement(IForceMovement movement);
        /// <summary>
        ///     Checks if this creature is within specified range to other creature.
        /// </summary>
        /// <param name="otherCreature">The other creature.</param>
        /// <param name="range">The range.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool WithinRange(ICreature otherCreature, int range);
        /// <summary>
        ///     Withins the range.
        /// </summary>
        /// <param name="otherLocation">The other location.</param>
        /// <param name="otherSize">Size of the other.</param>
        /// <param name="range">The range.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool WithinRange(ILocation otherLocation, int otherSize, int range);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="killer"></param>
        void OnKilledBy(ICreature killer);
        /// <summary>
        ///     Reset's creature facing.
        /// </summary>
        void ResetFacing();
        /// <summary>
        ///     Face's specific creature, can be null however if null is intentional
        ///     then it is encouraged to use ResetFacing().
        /// </summary>
        /// <param name="creature">The creature.</param>
        void FaceCreature(ICreature creature);
        /// <summary>
        ///     Applies standart state with immunity type argument.
        ///     If this creature has a state that is same type as immunityType then
        ///     this method does no effect and returns false.
        /// </summary>
        /// <param name="state">State which should be applied.</param>
        /// <param name="immunityType">Type of the immunity.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool ApplyStandardState(IState state, StateType immunityType);
        /// <summary>
        /// Get's if this character should be rendered for given character.
        /// </summary>
        /// <param name="viewer">Character for which this character should be rendered.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool ShouldBeRenderedFor(ICharacter viewer);
        /// <summary>
        /// Get's if this character should be rendered for given npc.
        /// </summary>
        /// <param name="viewer">NPC for which this character should be rendered.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool ShouldBeRenderedFor(INpc viewer);
        /// <summary>
        ///     Respawn's this creature ( Normalises statistics, moves to spawn point)
        ///     This also calls the OnSpawn method.
        /// </summary>
        void Respawn();
        /// <summary>
        ///     Get's called when creature dies.
        /// </summary>
        void OnDeath();
        /// <summary>
        /// Get's if this creature has specific state.
        /// </summary>
        /// <param name="type">Type of the state.</param>
        /// <returns><c>true</c> if the specified type has state; otherwise, <c>false</c>.</returns>
        bool HasState(StateType type);
        /// <summary>
        ///     Tick used for updating rendering information.
        /// </summary>
        Task MajorClientUpdateTickAsync();
        /// <summary>
        ///     Tick used for generating update cycle info.
        /// </summary>
        Task MajorClientPrepareUpdateTickAsync();
        /// <summary>
        ///     Tick used for cleaning up rendering information.
        /// </summary>
        Task MajorClientUpdateResetTickAsync();
        /// <summary>
        ///     Tick used for various content processing.
        /// </summary>
        void MajorUpdateTick();
        /// <summary>
        /// Interrupts the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        void Interrupt(object source);
        /// <summary>
        /// This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        void OnTargetKilled(ICreature target);

        /// <summary>
        ///     Add's specific state to creature.
        ///     If this creature already contains same type state then
        ///     the one with longest expiry time remains.
        /// </summary>
        /// <param name="state">The state.</param>
        void AddState(IState state);
        /// <summary>
        ///     Remove's specific state from creature.
        /// </summary>
        /// <param name="type">The type.</param>
        void RemoveState(StateType type);
        /// <summary>
        ///     Speak's specific text.
        /// </summary>
        /// <param name="text">Text which should be spoken.</param>
        void Speak(string text);
        /// <summary>
        ///     Stun's this creature.
        /// </summary>
        /// <param name="ticks">Amount of ticks creature will remain stunned.</param>
        void Stun(int ticks);

        /// <summary>
        ///     Freeze's this creature.
        /// </summary>
        /// <param name="ticks">Amount of ticks creature will remain frozen.</param>
        /// <param name="immunityTicks">Amount of ticks creature will have immunity for freezing.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool Freeze(int ticks, int immunityTicks);
        /// <summary>
        ///     Poison's this creature by given amount.
        ///     If amount is lower than 10 the creature is then unpoisoned.
        /// </summary>
        /// <param name="amount">Amount of poison strength.</param>
        /// <returns>If creature was poisoned sucessfully.</returns>
        bool Poison(short amount);
        /// <summary>
        /// Queues the graphic.
        /// </summary>
        /// <param name="graphic">The graphic.</param>
        void QueueGraphic(IGraphic graphic);
        /// <summary>
        /// Queues the animation.
        /// </summary>
        /// <param name="animation">The animation.</param>
        void QueueAnimation(IAnimation animation);
        /// <summary>
        /// Queue's specific hitsplat for this creature.
        /// </summary>
        /// <param name="splat">Splat which should be rendered.</param>
        void QueueHitSplat(IHitSplat splat);
        /// <summary>
        /// Queue's specific hitbar for this creature.
        /// </summary>
        /// <param name="hitBar"></param>
        void QueueHitBar(IHitBar hitBar);
        /// <summary>
        /// Queue's task to be performed.
        /// </summary>
        /// <param name="task">The task.</param>
        IRsTaskHandle QueueTask(ITaskItem task);
        /// <summary>
        /// Queue's task to be performed.
        /// </summary>
        /// <param name="task"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        IRsTaskHandle<TResult> QueueTask<TResult>(ITaskItem<TResult> task);
        /// <summary>
        ///     Turns to specific coordinates.
        ///     Given location MUST be in current gamemap bounds, otherwise
        ///     expect wrong facing directions.
        ///     Sizes can't be lower THAN 1.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="tileSizeX">The tile size X.</param>
        /// <param name="tileSizeY">The tile size Y.</param>
        void FaceLocation(ILocation location, int tileSizeX = 1, int tileSizeY = 1);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="glow"></param>
        void QueueGlow(IGlow glow);
        /// <summary>
        ///     Determines whether this creature has a certain event handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <returns></returns>
        bool HasEventHandler<TEventType>() where TEventType : ICreatureEvent;
        /// <summary>
        ///     Registers the event handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        EventHappened RegisterEventHandler<TEventType>(EventHappened<TEventType> handler) where TEventType : ICreatureEvent;
        /// <summary>
        ///     Unregisters the event handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">Handler which should be unregistered.</param>
        /// <returns></returns>
        bool UnregisterEventHandler<TEventType>(EventHappened handler) where TEventType : ICreatureEvent;
        /// <summary>
        /// Get's called when entity is registered to world.
        /// </summary>
        Task OnRegistered();
    }
}
