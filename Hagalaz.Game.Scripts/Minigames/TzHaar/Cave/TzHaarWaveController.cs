using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Logic;
using Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.Dialogues;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave
{
    public class TzHaarWaveController : NpcWaveController
    {
        private readonly ICharacter _character;
        private readonly IGameMessageService _gameMessaging;

        public TzHaarWaveController(
            ICharacter character, IGameMessageService gameMessaging, INpcBuilder npcBuilder, IEnumerable<WaveDto> definitions,
            ILocation[] spawnLocations) : base(npcBuilder, definitions, spawnLocations)
        {
            _character = character;
            _gameMessaging = gameMessaging;
        }

        public override void SpawnNextWave()
        {
            base.SpawnNextWave();
            if (CurrentWaveId + 1 > FinalWaveId)
            {
                var dialog = _character.ServiceProvider.GetRequiredService<JadDialogue>();
                _character.Widgets.OpenDialogue(dialog, false);
            }

            var script = _character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
            _character.Widgets.OpenWidget(316, _character.GameClient.IsScreenFixed ? 49 : 69, 1, script, false);
            _character.Configurations.SendStandardConfiguration(639, CurrentWaveId);
        }

        public override void StopWaves()
        {
            base.StopWaves();
            _character.Movement.Teleport(Teleport.Create(TzHaarConstants.CaveEntrance));
            if (CurrentWaveId <= 1)
            {
                var dialog = _character.ServiceProvider.GetRequiredService<QuitDialogue>();
                _character.Widgets.OpenDialogue(dialog, false);
                return;
            }

            //var database = ServiceLocator.Current.GetInstance<ISqlDatabaseManager>();
            if (CurrentWaveId >= FinalWaveId)
            {
                var dialog = _character.ServiceProvider.GetRequiredService<WinDialogue>();
                _character.Widgets.OpenDialogue(dialog, false);
                // fire cape reward
                _character.Inventory.TryAddItems(_character, [(6570, 1)], out _);
                // TODO
                _gameMessaging.MessageAsync(_character.DisplayName + " has just completed the Fight Caves!", GameMessageType.WorldSpecific);
                //await database.ExecuteAsync(new ActivityLogQuery(_character.MasterId, "Fight Caves", "I completed the Fight Caves and earned a Fire Cape."));
            }
            else
            {
                var dialog = _character.ServiceProvider.GetRequiredService<LoseDialogue>();
                _character.Widgets.OpenDialogue(dialog, false);
                // TODO
                //await database.ExecuteAsync(new ActivityLogQuery(_character.MasterId, "Fight Caves", "I fought in the Fight Caves."));
            }

            // tokkul reward
            _character.Inventory.TryAddItems(_character, [(6529, (int)(CurrentWaveId * 6 * (CurrentWaveId * 0.34)))], out _);
        }
    }
}