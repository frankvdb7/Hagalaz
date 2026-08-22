using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Services.GameWorld.Model.Maps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Represents a non-player creature entity.
    /// </summary>
    public partial class Npc : Creature, INpc
    {
        /// <summary>
        /// The event manager
        /// </summary>
        public IEventManager EventManager { get; }

        /// <summary>
        /// The NPC's owner definitions.
        /// </summary>
        /// <value>The definition.</value>
        public INpcDefinition Definition { get; private set; } = default!;

        /// <summary>
        /// Contains npc script.
        /// </summary>
        /// <value>The script.</value>
        public INpcScript Script { get; }

        /// <summary>
        /// Contains npc rendering information.
        /// </summary>
        /// <value>The rendering information.</value>
        public INpcRenderInformation RenderInformation { get; }

        /// <summary>
        /// Contains npc statistics.
        /// </summary>
        /// <value>The statistics.</value>
        public INpcStatistics Statistics { get; }

        /// <summary>
        /// Contains npc appereance.
        /// </summary>
        /// <value>The appereance.</value>
        public INpcAppearance Appearance { get; }

        /// <summary>
        /// Contains npc bounds.
        /// </summary>
        /// <value>The bounds.</value>
        public IBounds Bounds { get; }

        /// <summary>
        /// Gets the path finder.
        /// </summary>
        /// <value>
        /// The path finder.
        /// </value>
        public override IPathFinder PathFinder => Script.GetPathfinder();

        /// <summary>
        /// Contains the spawn direction.
        /// </summary>
        /// <value>The spawn direction.</value>
        public DirectionFlag SpawnFaceDirection { get; }

        public override int Size => Appearance.Size;

        public override DirectionFlag FaceDirection => LastLocation == null ? SpawnFaceDirection : base.FaceDirection;

        public Npc(
            IServiceScope serviceScope, ILocation defaultLocation, ILocation? minimumLocation, ILocation? maximumLocation,
            Func<INpc, INpcScript> scriptFactory,
            DirectionFlag? spawnFaceDirection, INpcDefinition definition,
            IEventManager eventManager,
            IScopedGameMediator mediator,
            ISmartPathFinder smartPathFinder,
            IMapRegionService mapRegionService,
            IProjectilePathFinder projectilePathFinder,
            IOptions<CombatOptions> combatOptions,
            IHitSplatBuilder hitSplatBuilder,
            INpcService npcService,
            ILootService lootService,
            ILootGenerator lootGenerator,
            IGroundItemBuilder groundItemBuilder)
            : base(serviceScope)
        {
            EventManager = eventManager;
            Viewport = new Viewport(this, mapRegionService, MapSize.Small);
            Movement = new Movement(this, smartPathFinder, mediator);

            minimumLocation ??= defaultLocation.Translate(-9, -9, 0);
            maximumLocation ??= defaultLocation.Translate(9, 9, 0);

            Location = defaultLocation.Clone();
            SetDefinition(definition);
            RenderInformation = new NpcRenderInformation(this);
            Bounds = new Bounds(definition.BoundsType, defaultLocation.Clone(), minimumLocation, maximumLocation);
            Statistics = new NpcStatistics(this, eventManager, hitSplatBuilder);
            Appearance = new NpcAppearance(this, npcService);
            Combat = new NpcCombat(this, npcService, lootService, lootGenerator, groundItemBuilder, projectilePathFinder,
                smartPathFinder, combatOptions, hitSplatBuilder);
            SpawnFaceDirection = spawnFaceDirection is DirectionFlag.None or null
                ? DirectionHelper.GetNpcFaceDirection(definition.SpawnFaceDirection)
                : spawnFaceDirection.Value;
            Script = scriptFactory(this);
        }

        /// <summary>
        /// Set's name to this npc.
        /// </summary>
        /// <param name="displayName">Display name of this npc.</param>
        public void SetDisplayName(string displayName)
        {
            if (DisplayName == displayName || Name == displayName)
            {
                return;
            }

            DisplayName = displayName;
            RenderInformation.ScheduleFlagUpdate(UpdateFlags.SetDisplayName);
        }

        private void SetDefinition(INpcDefinition definition)
        {
            Definition = definition;
            Name = definition.Name;
            DisplayName = definition.DisplayName;
        }

        /// <summary>
        /// Get's called when npc is registered.
        /// </summary>
        public override async Task OnRegistered()
        {
            // initialize the most important drawing logic first
            RenderInformation.OnRegistered();
            await base.OnRegistered();
            Script.OnCreate();
            if (Definition.WalksRandomly && Definition.BoundsType != BoundsType.Static)
            {
                QueueTask(new NpcRandomWalkTask(this));
            }
        }

        /// <summary>
        /// Get's called when npc is unregistered.
        /// </summary>
        protected override void OnDestroy()
        {
            EventManager.SendEvent(new CreatureDestroyedEvent(this));
            Script.OnDestroy();
            UnregisterEventHandlers();
        }

        /// <summary>
        /// Get's if this npc can be
        /// destroyed automatically.
        /// </summary>
        /// <returns><c>true</c> if this instance can destroy; otherwise, <c>false</c>.</returns>
        public override bool CanDestroy() => Script.CanDestroy();

        /// <summary>
        /// Get's if this npc can be suspended.
        /// </summary>
        /// <returns><c>true</c> if this instance can suspend; otherwise, <c>false</c>.</returns>
        public override bool CanSuspend() => Script.CanSuspend();

        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the cript type.</typeparam>
        /// <returns></returns>
        public TScriptType? GetScript<TScriptType>() where TScriptType : class, INpcScript => Script as TScriptType;

        /// <summary>
        /// Determines whether this instance has script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the cript type.</typeparam>
        /// <returns></returns>
        public bool HasScript<TScriptType>() where TScriptType : class, INpcScript
        {
            var type = typeof(TScriptType);
            return type.IsAssignableFrom(Script.GetType());
        }

        public bool TryGetScript<TScriptType>([NotNullWhen(true)] out TScriptType? script) where TScriptType : class, INpcScript
        {
            script = GetScript<TScriptType>();
            return script != null;
        }

        public override string ToString() => $"npc[name={DisplayName},id={Definition.Id},index={Index},loc=({Location})]";
    }
}
