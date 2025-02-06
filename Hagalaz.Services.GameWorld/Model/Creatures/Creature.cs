using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Events.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    public abstract class Creature : ICreature
    {
        private readonly ICreatureTaskService _taskService = default!;
        private readonly List<IHitSplat> _renderedHitSplats = new(sbyte.MaxValue);
        private readonly List<IHitBar> _renderedHitBars = new(sbyte.MaxValue);
        private readonly Queue<IAnimation> _queuedAnimations = new();
        private readonly Queue<IGraphic> _queuedGraphics = new();
        protected readonly Dictionary<StateType, IState> States = new();
        private Dictionary<Type, List<EventHappened>> _registeredEventHandlers = new();
        private CreatureUpdateState _updateState = CreatureUpdateState.Initializing;
        private readonly IServiceScope _serviceScope = default!;

        public bool IsDestroyed { get; private set; }

        /// <summary>
        ///     Gets or sets The unique client slot id given at creature entry.
        /// </summary>
        /// <value>The index.</value>
        public int Index { get; set; }

        /// <summary>
        ///     Gets the name of the creature.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Contains the display name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        ///     Contains creature's last location , can be null.
        /// </summary>
        /// <value>The last location.</value>
        public ILocation? LastLocation { get; private set; }

        /// <summary>
        ///     Gets or sets the location of the creature.
        /// </summary>
        /// <value>The location.</value>
        public ILocation Location { get; protected set; } = default!;

        /// <summary>
        /// Get's size of creature.
        /// </summary>
        /// <returns></returns>
        public abstract int Size { get; }

        /// <summary>
        ///     Contains this creature area.
        ///     Only available when creature is living.
        /// </summary>
        /// <value>The area.</value>
        public IArea Area { get; private set; } = default!;

        /// <summary>
        ///     Contains creature game map, this must be updated manually.
        /// </summary>
        /// <value>The game map.</value>
        public IViewport Viewport { get; protected set; } = default!;

        /// <summary>
        ///     Contains creature movement used for teleporting and walking.
        /// </summary>
        /// <value>The movement.</value>
        public IMovement Movement { get; protected set; } = default!;

        /// <summary>
        ///     Contains creature combat.
        /// </summary>
        /// <value>The combat.</value>
        public ICreatureCombat Combat { get; protected set; } = default!;

        /// <summary>
        ///     Contains entity map region.
        /// </summary>
        /// <value>The region.</value>
        public IMapRegion Region
        {
            get
            {
                if (Location == null)
                {
                    throw new InvalidOperationException($"{nameof(Location)} is not initialized yet.");
                }
                return MapRegionService.GetOrCreateMapRegion(Location.RegionId, Location.Dimension, false);
            }
        }

        /// <summary>
        ///     Contains creature to which this creature is facing ,
        ///     can be null.
        /// </summary>
        /// <value>The faced creature.</value>
        public ICreature? FacedCreature { get; private set; }

        /// <summary>
        ///     Gets the path finder.
        /// </summary>
        /// <value>
        ///     The path finder.
        /// </value>
        public abstract IPathFinder PathFinder { get; }

        public IMapRegionService MapRegionService => _serviceScope.ServiceProvider.GetRequiredService<IMapRegionService>();

        /// <summary>
        ///     Contains faced X coordinate.
        ///     -1 if no specific location is faced.
        /// </summary>
        /// <value>The turned to X.</value>
        public int TurnedToX { get; private set; } = -1;

        /// <summary>
        ///     Contains faced Y coordinate.
        ///     -1 if no specific location is faced.
        /// </summary>
        /// <value>The turned to Y.</value>
        public int TurnedToY { get; private set; } = -1;

        /// <summary>
        ///     Contains creature speaking text or null if it isn't speaking
        ///     anything this tick.
        /// </summary>
        /// <value>The speaking text.</value>
        public string? SpeakingText { get; private set; }

        /// <summary>
        ///     Contains rendered nonstandart movement.
        /// </summary>
        /// <value>The rendered nonstandart movement.</value>
        public IForceMovement? RenderedNonstandardMovement { get; private set; }

        /// <summary>
        ///     Contains rendered glow.
        /// </summary>
        /// <value>The rendered glow.</value>
        public IGlow? RenderedGlow { get; private set; }

        /// <summary>
        ///     Contains rendered hitsplats.
        /// </summary>
        /// <value>The rendered hit splats.</value>
        public IReadOnlyList<IHitSplat> RenderedHitSplats => _renderedHitSplats;

        /// <summary>
        ///     Contains  the rendered hit bars.
        /// </summary>
        /// <value>
        ///     The rendered hit bars.
        /// </value>
        public IReadOnlyList<IHitBar> RenderedHitBars => _renderedHitBars;

        public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;
        public IGameMediator Mediator => ServiceProvider.GetRequiredService<IScopedGameMediator>();


        public Creature(IServiceScope serviceScope)
        {
            _serviceScope = serviceScope;
            _taskService = serviceScope.ServiceProvider.GetRequiredService<ICreatureTaskService>();
        }

        /// <summary>
        ///     Executes routine procedures.
        /// </summary>
        protected abstract void ContentTick();

        /// <summary>
        ///     Prepares for updating procedures.
        /// </summary>
        protected abstract void UpdatesPrepareTick();

        /// <summary>
        ///     Executes updating procedures such as sending update packets.
        /// </summary>
        protected abstract Task UpdateTick();

        /// <summary>
        ///     Executes reseting procedures such as reseting update flags.
        /// </summary>
        protected abstract void ResetTick();

        /// <summary>
        ///     Get's if creature can be destroyed.
        /// </summary>
        /// <returns><c>true</c> if this instance can destroy; otherwise, <c>false</c>.</returns>
        public abstract bool CanDestroy();

        /// <summary>
        ///     Happens when creature is destroyed.
        /// </summary>
        protected abstract void OnDestroy();

        /// <summary>
        ///     Get's if creature can be suspended.
        /// </summary>
        /// <returns><c>true</c> if this instance can suspend; otherwise, <c>false</c>.</returns>
        public abstract bool CanSuspend();

        /// <summary>
        ///     This method get's called when creature
        ///     is about to be destroyed, and gives
        ///     opportunity to release all taken resources.
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                throw new InvalidOperationException($"{this} already destroyed!");
            }
            _updateState = CreatureUpdateState.Destroyed;
            IsDestroyed = true;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Location != null)
            {
                RemoveFromRegion(Region);
            }
            Area?.OnCreatureExitArea(this);
            OnDestroy();
            _serviceScope.Dispose();
        }

        /// <summary>
        ///     Get's called when creature is
        ///     registered to the world and can be started to be linked to regions.
        /// </summary>
        protected void OnInit()
        {
            OnSpawn();
            _updateState = CreatureUpdateState.Idle;
        }

        /// <summary>
        /// Get's called when entity is registered to world.
        /// </summary>
        public virtual Task OnRegistered()
        {
            Viewport.RebuildView();
            SetLocation(Location, true, true);

            foreach (var state in States.Values)
                state.Script.OnStateAdded(state, this);

            OnInit();
            return Task.CompletedTask;
        }

        public void SetLocation(ILocation location, bool forceRegionUpdate = false, bool firstUpdate = false)
        {
            if (!firstUpdate)
            {
                LastLocation = Location;
            }
            Location = location;

            if (firstUpdate || forceRegionUpdate || LastLocation != null && (LastLocation.RegionId != Location.RegionId || LastLocation?.Dimension != Location.Dimension))
            {
                if (LastLocation != null)
                {
                    var lastRegion = MapRegionService.GetOrCreateMapRegion(LastLocation.RegionId, LastLocation.Dimension, false);
                    RemoveFromRegion(lastRegion);
                }
                var region = MapRegionService.GetOrCreateMapRegion(Location.RegionId, Location.Dimension, true);
                AddToRegion(region);

                OnRegionChange();
            }

            OnLocationChange(LastLocation);
            UpdateArea();
        }

        /// <summary>
        ///     Happens when creature location is changed.
        /// </summary>
        /// <param name="oldLocation">The old location.</param>
        protected abstract void OnLocationChange(ILocation? oldLocation);

        /// <summary>
        ///     Notifies entity about region link.
        /// </summary>
        protected abstract void OnRegionChange();

        /// <summary>
        ///     Notifies creature that it must add itself
        ///     to new MapRegion.
        /// </summary>
        /// <param name="newRegion">The new region.</param>
        protected abstract void AddToRegion(IMapRegion newRegion);

        /// <summary>
        ///     Notifies creature that it must remove itself from
        ///     given region.
        /// </summary>
        /// <param name="region">The region.</param>
        protected abstract void RemoveFromRegion(IMapRegion region);

        /// <summary>
        ///     Informs creature that it's movement type have been changed.
        /// </summary>
        /// <param name="newtype">The newtype.</param>
        public abstract void MovementTypeChanged(MovementType newtype);

        /// <summary>
        ///     Inform's client that specific movement type was enabled for this tick only.
        /// </summary>
        /// <param name="type">The type.</param>
        public abstract void TemporaryMovementTypeEnabled(MovementType type);

        /// <summary>
        ///     Get's called when creature is spawned.
        /// </summary>
        public abstract void OnSpawn();

        /// <summary>
        ///     Get's called when creature dies.
        /// </summary>
        public abstract void OnDeath();

        /// <summary>
        ///     Get's called when killed by a creature.
        /// </summary>
        /// <param name="killer">The killer.</param>
        public abstract void OnKilledBy(ICreature killer);

        /// <summary>
        ///     This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        public abstract void OnTargetKilled(ICreature target);

        /// <summary>
        ///     Queues animation for this creature.
        /// </summary>
        /// <param name="animation">Animation which should be queued.</param>
        public void QueueAnimation(IAnimation animation) => _queuedAnimations.Enqueue(animation);

        /// <summary>
        ///     Queues graphic for this creature.
        /// </summary>
        /// <param name="graphic">Graphic which should be queued.</param>
        public void QueueGraphic(IGraphic graphic) => _queuedGraphics.Enqueue(graphic);

        /// <summary>
        ///     Get's count of queued animations.
        /// </summary>
        /// <value></value>
        public int QueuedAnimationsCount => _queuedAnimations.Count;

        /// <summary>
        ///     Get's count of queued graphics.
        /// </summary>
        /// <value></value>
        public int QueuedGraphicsCount => _queuedGraphics.Count;


        /// <summary>
        ///     Take's animation from queue.
        /// </summary>
        /// <returns>Animation.</returns>
        public IAnimation TakeAnimation() => _queuedAnimations.Dequeue();

        /// <summary>
        ///     Take's graphic from queue.
        /// </summary>
        /// <returns>Graphic.</returns>
        public IGraphic TakeGraphic() => _queuedGraphics.Dequeue();

        /// <summary>
        ///     Get's called when creature faces specific creature.
        ///     If creature is null then creature is not facing anything anymore.
        /// </summary>
        /// <param name="creature">The creature.</param>
        protected abstract void CreatureFaced(ICreature? creature);

        /// <summary>
        ///     Get's called when creatures faces to specific direction.
        ///     If x and y is -1 then it means it doesn't face any specific direction anymore.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        protected abstract void TurnedTo(int x, int y);

        /// <summary>
        ///     Happens when specific text is being spoken by this creature.
        /// </summary>
        /// <param name="text">Text which is being spoken.</param>
        protected abstract void TextSpoken(string text);

        /// <summary>
        ///     Get's called when specific hitsplat is about the rendered.
        /// </summary>
        /// <param name="splat">Splat which should be rendered.</param>
        protected abstract void HitSplatRendered(IHitSplat splat);

        /// <summary>
        ///     Hits the bar rendered.
        /// </summary>
        /// <param name="bar">The bar.</param>
        protected abstract void HitBarRendered(IHitBar bar);

        /// <summary>
        ///     Get's if this creature should be rendered for specific character.
        /// </summary>
        /// <param name="viewer">The character.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public abstract bool ShouldBeRenderedFor(ICharacter viewer);

        /// <summary>
        ///     Get's if this creature should be rendered for given npc.
        /// </summary>
        /// <param name="viewer">NPC for which this creature should be rendered.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public abstract bool ShouldBeRenderedFor(INpc viewer);

        /// <summary>
        ///     Get's called when nonstandart movement is about to be rendered.
        /// </summary>
        /// <param name="movement">Movement which should be rendered.</param>
        protected abstract void NonstandardMovementRendered(IForceMovement movement);

        /// <summary>
        ///     Get's called when specific glow is about to be rendered.
        /// </summary>
        /// <param name="glow">Glow which should be rendered.</param>
        protected abstract void GlowRendered(IGlow glow);

        /// <summary>
        ///     Respawn's this creature ( Normalises statistics, moves to spawn point)
        ///     This also calls the OnSpawn method.
        /// </summary>
        public abstract void Respawn();

        /// <summary>
        ///     Interrupts current jobs.
        /// </summary>
        /// <param name="source">
        ///     Object which performed the interruption,
        ///     this parameter can be null , but it is not recommended to do so.
        ///     Best use would be to set the invoker class instance ('this') or any other related object
        ///     if invoker is static as source.
        /// </param>
        /// <returns>If something was interrupted.</returns>
        public abstract void Interrupt(object source);

        /// <summary>
        ///     Poison's this creature by given amount.
        ///     If amount is lower than 10 the creature is then unpoisoned.
        /// </summary>
        /// <param name="amount">Amount of poison strength.</param>
        /// <returns>If creature was poisoned successfully.</returns>
        public abstract bool Poison(short amount);

        /// <summary>
        ///     Freezes this creature.
        /// </summary>
        /// <param name="ticks">Amount of ticks creature will remain frozen.</param>
        /// <param name="immunityTicks">Amount of ticks creature will have immunity for freezing.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Freeze(int ticks, int immunityTicks)
        {
            if (HasState(StateType.ResistFreeze))
                return false;
            AddState(new State(StateType.Frozen, ticks));
            AddState(new State(StateType.ResistFreeze, immunityTicks));
            return true;
        }

        /// <summary>
        ///     Stun's this creature.
        /// </summary>
        /// <param name="ticks">Amount of ticks creature will remain stunned.</param>
        public void Stun(int ticks) => AddState(new State(StateType.Stun, ticks));

        /// <summary>
        ///     Applies standard state with immunity type argument.
        ///     If this creature has a state that is same type as immunityType then
        ///     this method does no effect and returns false.
        /// </summary>
        /// <param name="state">State which should be applied.</param>
        /// <param name="immunityType">Type of the immunity.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ApplyStandardState(IState state, StateType immunityType)
        {
            if (HasState(immunityType))
                return false;
            AddState(state);
            return true;
        }

        /// <summary>
        ///     Speak's specific text.
        /// </summary>
        /// <param name="text">Text which should be spoken.</param>
        public void Speak(string text)
        {
            SpeakingText = text;
            TextSpoken(text);
        }

        /// <summary>
        ///     Face's specific creature
        /// </summary>
        /// <param name="creature">The creature.</param>
        public void FaceCreature(ICreature creature)
        {
            if (!Viewport.VisibleCreatures.Contains(creature))
            {
                return;
            }
            if (FacedCreature == creature)
            {
                return;
            }
            FacedCreature = creature;
            CreatureFaced(creature);
        }

        /// <summary>
        ///     Turn's to specific coordinates.
        ///     Given location MUST be in current gamemap bounds, otherwise
        ///     expect wrong facing directions.
        ///     Sizes can't be lower THAN 1.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="tileSizeX">The tile size X.</param>
        /// <param name="tileSizeY">The tile size Y.</param>
        public void FaceLocation(ILocation location, int tileSizeX = 1, int tileSizeY = 1)
        {
            var targetMapX = -1;
            var targetMapY = -1;
            Viewport.GetLocalPosition(location, ref targetMapX, ref targetMapY);
            FaceLocation(targetMapX * 2 + tileSizeX, targetMapY * 2 + tileSizeY);
        }

        /// <summary>
        ///     Turn's to specific coordinates.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <exception cref="System.Exception"></exception>
        public void FaceLocation(int x, int y)
        {
            TurnedToX = x;
            TurnedToY = y;
            TurnedTo(x, y);
        }

        /// <summary>
        ///     Render's nonstandart movement.
        /// </summary>
        /// <param name="movement">Movement which should be rendered.</param>
        public void QueueForceMovement(IForceMovement movement)
        {
            RenderedNonstandardMovement = movement;
            NonstandardMovementRendered(movement);
        }

        /// <summary>
        ///     Render's glow.
        /// </summary>
        /// <param name="glow">Glow which should be rendered.</param>
        public void QueueGlow(IGlow glow)
        {
            RenderedGlow = glow;
            GlowRendered(glow);
        }

        /// <summary>
        ///     Reset's creature facing.
        /// </summary>
        public void ResetFacing()
        {
            if (FacedCreature != null)
            {
                FacedCreature = null;
                CreatureFaced(null);
            }
        }

        /// <summary>
        ///     Render's specific hitsplat for this creature.
        /// </summary>
        /// <param name="splat">Splat which should be rendered.</param>
        public void QueueHitSplat(IHitSplat splat)
        {
            _renderedHitSplats.Add(splat);
            HitSplatRendered(splat);
        }

        /// <summary>
        ///     Renders the hit bar.
        /// </summary>
        /// <param name="bar">The bar.</param>
        public void QueueHitBar(IHitBar bar)
        {
            _renderedHitBars.Add(bar);
            HitBarRendered(bar);
        }

        /// <summary>
        ///     Tick used for various content processing.
        /// </summary>
        public void MajorUpdateTick()
        {
            if (_updateState != CreatureUpdateState.Idle)
            {
                return;
            }

            _updateState = CreatureUpdateState.ServerUpdate;

            var faced = FacedCreature;
            if (faced != null && !Viewport.VisibleCreatures.Contains(faced))
                ResetFacing();
            ContentTick();
            Combat.Tick();
            TaskTick();
            ProcessStates();
            Movement.Tick();
        }

        /// <summary>
        ///     Tick used for generating update cycle info.
        /// </summary>
        public Task MajorClientPrepareUpdateTickAsync()
        {
            if (_updateState != CreatureUpdateState.ServerUpdate)
            {
                return Task.CompletedTask;
            }

            _updateState = CreatureUpdateState.ClientPrepareUpdate;

            UpdatesPrepareTick();
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Tick used for updating rendering information.
        /// </summary>
        public async Task MajorClientUpdateTickAsync()
        {
            if (_updateState != CreatureUpdateState.ClientPrepareUpdate)
            {
                return;
            }

            _updateState = CreatureUpdateState.ClientUpdate;

            await UpdateTick();
        }

        /// <summary>
        ///     Tick used for cleaning up rendering information.
        /// </summary>
        public Task MajorClientUpdateResetTickAsync()
        {
            if (_updateState != CreatureUpdateState.ClientUpdate)
            {
                return Task.CompletedTask;
            }

            _updateState = CreatureUpdateState.ClientUpdateReset;

            SpeakingText = null; // no longer speak same ;P
            RenderedNonstandardMovement = null; // no longer render same movement
            RenderedGlow = null; // no longer render same glow
            _renderedHitSplats.Clear(); // no longer render same hit splats
            _renderedHitBars.Clear(); // no longer render same hit bars
            TurnedToX = -1;
            TurnedToY = -1;
            Movement.Reset();
            ResetTick();
            _updateState = CreatureUpdateState.Idle;
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Queue's task to be performed.
        /// </summary>
        /// <param name="task">The task.</param>
        public IRsTaskHandle QueueTask(ITaskItem task)
        {
            _taskService.Schedule(task);
            return new RsTaskHandle(task);
        }

        public IRsTaskHandle<TResult> QueueTask<TResult>(ITaskItem<TResult> task)
        {
            _taskService.Schedule(task);
            return new RsTaskHandle<TResult>(task);
        }

        /// <summary>
        ///     Process queued actions.
        /// </summary>
        private void TaskTick() => _taskService.Tick();

        /// <summary>
        ///     Process creature states.
        /// </summary>
        private void ProcessStates()
        {
            foreach (var state in States.Values.ToList())
            {
                state.Tick();
                if (state.Removed)
                {
                    States.Remove(state.StateType);
                }
            }
        }

        /// <summary>
        ///     Get's if this creature has specific state.
        /// </summary>
        /// <param name="type">Type of the state.</param>
        /// <returns><c>true</c> if the specified type has state; otherwise, <c>false</c>.</returns>
        public bool HasState(StateType type) => States.ContainsKey(type);

        /// <summary>
        ///     Remove's specific state from creature.
        /// </summary>
        /// <param name="type">The type.</param>
        public void RemoveState(StateType type)
        {
            if (!States.ContainsKey(type))
            {
                return;
            }

            var state = States[type];
            States.Remove(type);
            state.Script.OnStateRemoved(state, this);
        }

        /// <summary>
        ///     Get's creature states.
        /// </summary>
        /// <returns>List{State}.</returns>
        public IEnumerable<IState> GetStates() => States.Values;

        /// <summary>
        ///     Gets the states.
        /// </summary>
        /// <returns></returns>
        public Dictionary<StateType, IState> GetDictionaryStates() => States;

        /// <summary>
        ///     Gets the state.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>State.</returns>
        public IState? GetState(StateType type) => States.ContainsKey(type) ? States[type] : null;

        /// <summary>
        ///     Add's specific state to creature.
        ///     If this creature already contains same type state then
        ///     the one with longest expiry time remains.
        /// </summary>
        /// <param name="state">The state.</param>
        public void AddState(IState state)
        {
            if (States.ContainsKey(state.StateType))
            {
                var other = States[state.StateType];
                if (state.RemoveDelay <= other.RemoveDelay)
                {
                    return;
                }

                States.Remove(state.StateType);
                States.Add(state.StateType, state);
            }
            else
            {
                States.Add(state.StateType, state);
                state.Script.OnStateAdded(state, this);
            }
        }

        /// <summary>
        ///     Updates the area.
        /// </summary>
        private void UpdateArea()
        {
            var oldArea = Area;
            var areaManager = ServiceProvider.GetRequiredService<IAreaService>();
            var newArea = areaManager.FindAreaByLocation(Location);
            if (oldArea == newArea)
            {
                return;
            }

            Area = newArea;
            oldArea?.OnCreatureExitArea(this);
            Area.OnCreatureEnterArea(this);
        }

        /// <summary>
        ///     Get's facing direction of this creature.
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public virtual DirectionFlag FaceDirection
        {
            get
            {
                ILocation sourceLocation;
                ILocation targetLocation;
                var facingTo = FacedCreature;
                if (facingTo != null)
                {
                    sourceLocation = Location;
                    targetLocation = facingTo.Location;
                }
                else if (LastLocation == null)
                {
                    return DirectionFlag.South; // default facing is south.
                }
                else
                {
                    sourceLocation = LastLocation;
                    targetLocation = Location;
                }

                return sourceLocation.GetDirection(targetLocation);
            }
        }

        /// <summary>
        ///     Checks if this creature is within specified range to other creature.
        /// </summary>
        /// <param name="otherCreature">The other creature.</param>
        /// <param name="range">The range.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool WithinRange(ICreature otherCreature, int range) => WithinRange(otherCreature.Location, otherCreature.Size, range);

        /// <summary>
        ///     Withins the range.
        /// </summary>
        /// <param name="otherLocation">The other location.</param>
        /// <param name="otherSize">Size of the other.</param>
        /// <param name="range">The range.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool WithinRange(ILocation otherLocation, int otherSize, int range)
        {
            if (Location.Z != otherLocation.Z || Location.Dimension != otherLocation.Dimension)
                return false;

            var thisSize = Size;
            var myX = Location.X;
            var myY = Location.Y;
            var otherX = otherLocation.X;
            var otherY = otherLocation.Y;

            for (var x1 = 0; x1 < thisSize; x1++)
                for (var y1 = 0; y1 < thisSize; y1++)
                    for (var x2 = 0; x2 < otherSize; x2++)
                        for (var y2 = 0; y2 < otherSize; y2++)
                        {
                            var distance = (int)Game.Abstractions.Model.Location.GetDistance(myX + x1, myY + y1, otherX + x2, otherY + y2);
                            if (distance <= range)
                                return true;
                        }

            return false;
        }

        /// <summary>
        ///     Registers the event handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        public EventHappened RegisterEventHandler<TEventType>(EventHappened<TEventType> handler)
            where TEventType : ICreatureEvent
        {
            var handlerType = typeof(TEventType);
            if (_registeredEventHandlers == null)
                throw new Exception("Events can no longer be registered to this creature because UnregisterEventHandlers() was called!");

            bool HandleCreateEvent(TEventType e) => e.Target == this && handler.Invoke(e);

            var eventManager = ServiceProvider.GetRequiredService<IEventManager>();

            var eventHandle = eventManager.Listen<TEventType>(HandleCreateEvent);
            if (_registeredEventHandlers.ContainsKey(handlerType))
            {
                _registeredEventHandlers[handlerType].Add(eventHandle);
            }
            else
            {
                var events = new List<EventHappened>
                {
                    eventHandle
                };
                _registeredEventHandlers.Add(handlerType, events);
            }

            return eventHandle;
        }

        /// <summary>
        ///     Unregisters the event handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">Handler which should be unregistered.</param>
        /// <returns></returns>
        public bool UnregisterEventHandler<TEventType>(EventHappened handler)
            where TEventType : ICreatureEvent
        {
            var handlerType = typeof(TEventType);
            if (!_registeredEventHandlers.ContainsKey(handlerType) || !_registeredEventHandlers[handlerType].Remove(handler))
            {
                return false;
            }

            var eventManager = ServiceProvider.GetRequiredService<IEventManager>();
            return eventManager.StopListen<TEventType>(handler);
        }

        /// <summary>
        ///     Unregister's all events that this character listens to.
        /// </summary>
        protected void UnregisterEventHandlers()
        {
            var eventManager = ServiceProvider.GetRequiredService<IEventManager>();
            foreach (var type in _registeredEventHandlers.Keys)
                _registeredEventHandlers[type]
                    .ForEach(eventHappened => eventManager.StopListen(type, eventHappened));
            _registeredEventHandlers = null!;
        }

        /// <summary>
        ///     Determines whether this creature has a certain event handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <returns></returns>
        public bool HasEventHandler<TEventType>() where TEventType : ICreatureEvent
        {
            var type = typeof(TEventType);
            return _registeredEventHandlers.ContainsKey(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"creature[name={Name}, index={Index}]";
    }
}