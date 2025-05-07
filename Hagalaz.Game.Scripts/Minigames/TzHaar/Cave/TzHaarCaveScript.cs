using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Scripts.Logic;
using Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.Dialogues;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave
{
    /// <summary>
    /// </summary>
    public class TzHaarCaveScript : CharacterScriptBase
    {
        private NpcWaveController _waveController;
        private EventHappened? _caveKillHandler;
        private readonly IMapRegionService _regionManager;
        private readonly ITzHaarCaveService _tzHaarRepository;
        private readonly IGameMessageService _gameMessaging;
        private readonly INpcBuilder _npcBuilder;

        public TzHaarCaveScript(
            ICharacterContextAccessor contextAccessor, IMapRegionService regionManager, ITzHaarCaveService tzHaarRepository,
            IGameMessageService gameMessaging, INpcBuilder npcBuilder) : base(contextAccessor)
        {
            _regionManager = regionManager;
            _tzHaarRepository = tzHaarRepository;
            _gameMessaging = gameMessaging;
            _npcBuilder = npcBuilder;
        }

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
            if (!_regionManager.TryCreateDimension(out var dimension))
            {
                return;
            }

            Character.Movement.Teleport(Teleport.Create(Location.Create(Character.Location.X, Character.Location.Y, Character.Location.Z, dimension.Id)));
            _waveController = new TzHaarWaveController(Character,
                _gameMessaging,
                _npcBuilder,
                _tzHaarRepository.FindAllWaves().Result,
                [
                    Location.Create(4467, 5147, 0, dimension.Id),
                    Location.Create(4453, 5163, 0, dimension.Id),
                    Location.Create(4462, 5175, 0, dimension.Id),
                    Location.Create(4455, 5134, 0, dimension.Id),
                    Location.Create(4427, 5132, 0, dimension.Id),
                    Location.Create(4427, 5140, 0, dimension.Id),
                    Location.Create(4438, 5162, 0, dimension.Id),
                    Location.Create(4430, 5170, 0, dimension.Id)
                ]);

            var dto = Character.Profile.GetObject(TzHaarConstants.ProfileMinigamesTzhaarCaves, new TzHaarCaveDto());
            _waveController.CurrentWaveId = dto.CurrentWaveId;

            if (_waveController.CurrentWaveId == 0)
            {
                StartNewGame(); // no save data, so we start the game from the beginning.
            }

            _caveKillHandler = Character.RegisterEventHandler(new EventHappened<CreatureKillEvent>(e =>
            {
                if (e.Victim is not INpc npc)
                {
                    return false; // The event is not handled.
                }

                if (npc.Appearance.CompositeID == 2736 || npc.Appearance.CompositeID == 2737)
                {
                    var npcHealer1 = _npcBuilder
                        .Create()
                        .WithId(2738)
                        .WithLocation(npc.Location)
                        .Spawn();
                    var npcHealer2 = _npcBuilder
                        .Create()
                        .WithId(2738)
                        .WithLocation(npc.Location)
                        .Spawn();

                    _waveController.AddSpawn(npcHealer1);
                    _waveController.AddSpawn(npcHealer2);
                }

                _waveController.RemoveSpawn(npc);
                UpdateTzhaarCaveProfile();

                return false; // The event is not handled.
            }));
        }

        /// <summary>
        ///     Starts a new game.
        /// </summary>
        public void StartNewGame()
        {
            var dialog = Character.ServiceProvider.GetRequiredService<StartDialogue>();
            Character.Widgets.OpenDialogue(dialog, false);
            var allowWalkingEvent = Character.RegisterEventHandler(new EventHappened<WalkAllowEvent>(e =>
            {
                return true; // Dissallow walking
            }));
            var task = new LocationReachTask(Character,
                TzHaarConstants.CaveCenter.Copy(Character.Location.Dimension),
                success =>
                {
                    if (success)
                    {
                        _waveController.SpawnNextWave();
                    }

                    Character.UnregisterEventHandler<WalkAllowEvent>(allowWalkingEvent);
                },
                typeof(ICharacter)); // Prevent any interruption that would have prevented the task from executing.
            Character.QueueTask(task);
        }

        /// <summary>
        ///     Determines whether this instance [can be looted] the specified killer.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can be looted] the specified killer; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanBeLootedBy(ICreature killer) => false;

        /// <summary>
        ///     Called when this script is removed from the character.
        ///     By default this method does nothing.
        /// </summary>
        public override void OnRemove()
        {
            if (_caveKillHandler != null)
            {
                Character.UnregisterEventHandler<CreatureKillEvent>(_caveKillHandler);
            }

            _waveController.StopWaves();

            Character.Profile.SetObject(TzHaarConstants.ProfileMinigamesTzhaarCaves, new TzHaarCaveDto());
        }

        /// <summary>
        ///     Happens when character enters world.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnRegistered() => _waveController.SpawnNextWave();

        /// <summary>
        ///     Gets called when character is destroyed permanently.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDestroy()
        {
            if (_caveKillHandler != null)
            {
                Character.UnregisterEventHandler<CreatureKillEvent>(_caveKillHandler);
            }

            _waveController.StopWaves();
        }

        private void UpdateTzhaarCaveProfile() =>
            Character.Profile.SetObject(TzHaarConstants.ProfileMinigamesTzhaarCaves,
                new TzHaarCaveDto
                {
                    CurrentWaveId = _waveController.CurrentWaveId
                });
    }
}