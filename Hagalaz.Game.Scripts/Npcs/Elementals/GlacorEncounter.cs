using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Npcs.Elementals
{
    /// <summary>
    /// Coordinates the Glacor encounter's spawned Glacytes.
    /// </summary>
    internal sealed class GlacorEncounter
    {
        private readonly INpc _glacor;
        private readonly INpcBuilder _npcBuilder;
        private readonly List<TrackedGlacyte> _glacytes = [];
        private int _deadCount;

        public GlacorEncounter(INpc glacor, INpcBuilder npcBuilder)
        {
            _glacor = glacor;
            _npcBuilder = npcBuilder;
        }

        public bool GlacytesSpawned { get; private set; }

        public bool GlacytesDead => _deadCount >= 3;

        internal int GlacyteDeadCount => _deadCount;

        public int LastKilledGlacyteId { get; private set; }

        internal int TrackedGlacyteCount => _glacytes.Count;

        public event Action<INpc>? GlacyteDied;

        public void Begin()
        {
            _deadCount = 0;
            LastKilledGlacyteId = 0;
            GlacytesSpawned = true;
        }

        public void SetTarget(ICreature target)
        {
            foreach (var glacyte in _glacytes)
            {
                if (glacyte.Npc.IsDestroyed || glacyte.Npc.Combat.Target is not null)
                {
                    continue;
                }

                glacyte.Npc.QueueTask(new RsTask(() => glacyte.Npc.Combat.SetTarget(target), 1));
            }
        }

        public void SpawnGlacyte(
            int id,
            ILocation location,
            Type scriptType,
            Action<INpcScript>? configure = null)
        {
            var optional = _npcBuilder.Create()
                .WithId(id)
                .WithLocation(location);
            if (configure is null)
            {
                optional.WithScript(scriptType);
            }
            else
            {
                optional.WithScript(scriptType, configure);
            }

            var handle = optional.Spawn();
            var glacyte = handle.Npc;
            if (_glacor.Combat.Target is { } target)
            {
                glacyte.QueueTask(new RsTask(() => glacyte.Combat.SetTarget(target), 1));
            }

            var tracked = new TrackedGlacyte(handle, glacyte);
            tracked.TargetHandler = glacyte.RegisterEventHandler(new EventHappened<CreatureSetCombatTargetEvent>(e =>
            {
                if (_glacor.Combat.Target is null)
                {
                    _glacor.QueueTask(new RsTask(() => _glacor.Combat.SetTarget(e.CombatTarget), 1));
                }

                return false;
            }));
            tracked.DeathHandler = glacyte.RegisterEventHandler(new EventHappened<CreatureDiedEvent>(_ =>
            {
                _deadCount++;
                LastKilledGlacyteId = glacyte.Appearance.CompositeID;
                _glacytes.Remove(tracked);
                UnregisterHandlers(tracked);
                GlacyteDied?.Invoke(glacyte);
                return false;
            }));
            _glacytes.Add(tracked);
        }

        public void Clear()
        {
            foreach (var glacyte in _glacytes.ToArray())
            {
                UnregisterHandlers(glacyte);
                glacyte.Handle.Unregister();
            }

            _glacytes.Clear();
            _deadCount = 0;
            LastKilledGlacyteId = 0;
            GlacytesSpawned = false;
        }

        private static void UnregisterHandlers(TrackedGlacyte glacyte)
        {
            if (glacyte.TargetHandler is not null)
            {
                glacyte.Npc.UnregisterEventHandler<CreatureSetCombatTargetEvent>(glacyte.TargetHandler);
                glacyte.TargetHandler = null;
            }

            if (glacyte.DeathHandler is not null)
            {
                glacyte.Npc.UnregisterEventHandler<CreatureDiedEvent>(glacyte.DeathHandler);
                glacyte.DeathHandler = null;
            }
        }

        private sealed class TrackedGlacyte(INpcHandle handle, INpc npc)
        {
            public INpcHandle Handle { get; } = handle;
            public INpc Npc { get; } = npc;
            public EventHappened? TargetHandler { get; set; }
            public EventHappened? DeathHandler { get; set; }
        }
    }
}
