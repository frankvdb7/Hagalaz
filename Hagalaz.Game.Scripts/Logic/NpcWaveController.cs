using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Logic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Logic
{
    /// <summary>
    /// 
    /// </summary>
    public class NpcWaveController : IWaveController
    {
        /// <summary>
        /// The started.
        /// </summary>
        private bool _started;

        /// <summary>
        /// The current npcs.
        /// </summary>
        private readonly List<INpcHandle> _spawnedNpcs = [];

        /// <summary>
        /// The definitions.
        /// </summary>
        private readonly Dictionary<int, WaveDto> _definitions;

        private readonly INpcBuilder _npcBuilder;

        /// <summary>
        /// The spawn locations.
        /// </summary>
        private readonly ILocation[] _spawnLocations;

        /// <summary>
        /// Contains the current wave.
        /// </summary>
        /// <value>
        /// The current wave identifier.
        /// </value>
        public int CurrentWaveId { get; set; }

        /// <summary>
        /// Gets the last wave identifier.
        /// </summary>
        /// <value>
        /// The last wave identifier.
        /// </value>
        public int FinalWaveId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpcWaveController" /> class.
        /// </summary>
        /// <param name="npcBuilder"></param>
        /// <param name="definitions">The definitions.</param>
        /// <param name="spawnLocations">The spawn locations.</param>
        public NpcWaveController(INpcBuilder npcBuilder, IEnumerable<WaveDto> definitions, ILocation[] spawnLocations)
        {
            _definitions = definitions.ToDictionary(e => e.WaveId, e => e);
            _npcBuilder = npcBuilder;
            _spawnLocations = spawnLocations;
            FinalWaveId = _definitions.Keys.Max();
        }

        /// <summary>
        /// Nexts the wave.
        /// </summary>
        public virtual void SpawnNextWave()
        {
            if (!_started) return;

            CurrentWaveId++;

            ClearSpawns();
            if (!_definitions.TryGetValue(CurrentWaveId, out var definition))
            {
                StopWaves();
                return;
            }

            foreach (var npcWave in definition.Npcs)
            {
                for (var spawn = 0; spawn < npcWave.Count; spawn++)
                {
                    var spawnLocation = _spawnLocations[RandomStatic.Generator.Next(0, _spawnLocations.Length)];
                    var npc = _npcBuilder
                        .Create()
                        .WithId(npcWave.NpcId)
                        .WithLocation(spawnLocation)
                        .Spawn();
                    _spawnedNpcs.Add(npc);
                }
            }
        }

        /// <summary>
        /// Checks the next wave.
        /// </summary>
        private void CheckNextWave()
        {
            if (_spawnedNpcs.Count <= 0)
                SpawnNextWave();
        }

        /// <summary>
        /// Clears the wave.
        /// </summary>
        private void ClearSpawns()
        {
            foreach (var npc in _spawnedNpcs)
            {
                npc.Unregister();
            }
            _spawnedNpcs.Clear();
        }

        /// <summary>
        /// Ends this instance.
        /// </summary>
        public virtual void StopWaves()
        {
            if (!_started) return;

            _started = false;
            ClearSpawns();
        }

        /// <summary>
        /// Adds the spawned NPC.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        public void AddSpawn(INpcHandle npc)
        {
            if (!_spawnedNpcs.Contains(npc))
                _spawnedNpcs.Add(npc);
            CheckNextWave();
        }

        /// <summary>
        /// Removes the spawned NPC.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        public void RemoveSpawn(INpcHandle npc)
        {
            _spawnedNpcs.Remove(npc);
            CheckNextWave();
        }

        public void RemoveSpawn(INpc npc)
        {
            var handle = _spawnedNpcs.FirstOrDefault(e => e.Npc == npc);
            if (handle != null)
            {
                RemoveSpawn(handle);
            }
            CheckNextWave();
        }
    }
}