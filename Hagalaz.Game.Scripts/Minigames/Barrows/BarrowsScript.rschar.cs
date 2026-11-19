using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    /// <summary>
    /// </summary>
    internal class SpawnEntry
    {
        private readonly INpcBuilder _npcBuilder;
        private readonly IHintIconBuilder _hintIconBuilder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpawnEntry" /> class.
        /// </summary>
        /// <param name="npcID">The NPC identifier.</param>
        /// <param name="npcBuilder"></param>
        /// <param name="hintIconBuilder"></param>
        public SpawnEntry(int npcID, INpcBuilder npcBuilder, IHintIconBuilder hintIconBuilder)
        {
            NpcID = npcID;
            Killed = false;
            Spawn = null;
            _npcBuilder = npcBuilder;
            _hintIconBuilder = hintIconBuilder;
        }

        /// <summary>
        ///     Gets the NPC Id.
        /// </summary>
        /// <value>
        ///     The NPC Id.
        /// </value>
        public int NpcID { get; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="SpawnEntry" /> is killed.
        /// </summary>
        /// <value>
        ///     <c>true</c> if killed; otherwise, <c>false</c>.
        /// </value>
        public bool Killed { get; private set; }

        /// <summary>
        ///     Gets or sets the spawn.
        /// </summary>
        /// <value>
        ///     The spawn.
        /// </value>
        public INpcHandle? Spawn { get; private set; }

        /// <summary>
        ///     Sets the killed.
        /// </summary>
        public void SetKilled()
        {
            Killed = true;
            Spawn = null;
        }

        /// <summary>
        ///     Spawns the specified npc for the charcter.
        /// </summary>
        /// <param name="character">The character.</param>
        public void SpawnNpc(ICharacter character)
        {
            if (Spawn != null || Killed)
            {
                return;
            }

            var handle = _npcBuilder
                .Create()
                .WithId(NpcID)
                .WithLocation(character.Location)
                .Spawn();
            var npc = handle.Npc;
            npc.Speak("You dare to disturb my rest?!");
            var characterHandle = character.Handle;
            npc.QueueTask(new RsTask(() => npc.Combat.SetTarget(characterHandle), 1));

            var icon = _hintIconBuilder.Create().AtEntity(npc).Build();
            if (character.TryRegisterHintIcon(icon))
            {
                npc.RegisterEventHandler(new EventHappened<CreatureDestroyedEvent>(e =>
                {
                    if (e.Target == npc)
                    {
                        character.TryUnregisterHintIcon(icon);
                    }

                    return false; // let other events handle this too
                }));
            }

            Spawn = handle;
        }

        /// <summary>
        ///     Despawns this instance.
        /// </summary>
        public void Despawn()
        {
            Spawn?.Unregister();
            Spawn = null;
        }

        /// <summary>
        ///     Resets this instance.
        /// </summary>
        public void Reset()
        {
            Despawn();
            Killed = false;
        }
    }

    /// <summary>
    /// </summary>
    public class BarrowsScript : CharacterScriptBase
    {
        /// <summary>
        ///     Gets or sets a value indicating whether [in crypts].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [in crypts]; otherwise, <c>false</c>.
        /// </value>
        public bool InCrypts
        {
            get => _inCrypts;

            set
            {
                _inCrypts = value;
                if (!_inCrypts)
                {
                    _cryptTick = 0;
                }
                else
                {
                    SendCryptConfiguration();
                }
            }
        }

        public BarrowsScript(
            ICharacterContextAccessor contextAccessor, IItemService itemRepository, ILootService lootService, INpcBuilder npcBuilder,
            IHintIconBuilder hintIconBuilder)
            : base(contextAccessor)
        {
            _itemRepository = itemRepository;
            _lootService = lootService;
            _npcBuilder = npcBuilder;
            _hintIconBuilder = hintIconBuilder;
            LoadRun();
        }

        /// <summary>
        ///     The spawn entries.
        ///     0 = Ahrim.
        ///     1 = Dharok.
        ///     2 = Guthan.
        ///     3 = Karil.
        ///     4 = Torag.
        ///     5 = Verac.
        ///     6 = Akrisae.
        /// </summary>
        private readonly SpawnEntry[] _spawnEntries = new SpawnEntry[7];

        /// <summary>
        ///     The kill count.
        /// </summary>
        private int _killCount;

        private int _creatureLevels;

        /// <summary>
        ///     Gets the number of defeated brothers, derived from the authoritative killed flags.
        /// </summary>
        private int BrothersKilled => _spawnEntries.Count(e => e.Killed);

        /// <summary>
        ///     The barrows kill handler.
        /// </summary>
        private EventHappened? _barrowsKillHandler;

        /// <summary>
        ///     The barrow brother index of which the sarcophagus may lead to the tunnel.
        /// </summary>
        private int _tunnelIndex;

        private int _puzzleDoorIndex;

        private int _puzzleIndex;

        private bool _puzzleSolved;

        private Action? _onPuzzleSolved;

        /// <summary>
        ///     The crypt tick.
        /// </summary>
        private int _cryptTick;

        /// <summary>
        ///     The in crypts.
        /// </summary>
        private bool _inCrypts;

        /// <summary>
        ///     Wether the character has looted the chest.
        /// </summary>
        private bool _lootedChest;

        /// <summary>
        ///     The crypt start index.
        /// </summary>
        private int _cryptStartIndex;

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        /// <summary>
        ///     The loot manager
        /// </summary>
        private readonly ILootService _lootService;

        private readonly INpcBuilder _npcBuilder;
        private readonly IHintIconBuilder _hintIconBuilder;

        /// <summary>
        ///     Intentionally empty: the base constructor invokes this before the injected dependencies are assigned,
        ///     so the run is loaded from the constructor body instead (see <see cref="LoadRun" />).
        /// </summary>
        protected override void Initialize() { }

        private static BarrowsDto NewRun()
        {
            var (door, puzzle) = BarrowsPuzzle.New(RandomStatic.Generator.Next);
            return new BarrowsDto
            {
                TunnelIndex = RandomStatic.Generator.Next(0, BarrowsConstants.MoundCount), PuzzleDoorIndex = door, PuzzleIndex = puzzle
            };
        }

        /// <summary>
        ///     Config 452: one rope bit per entry (bits 6-9) plus a lock bit for each non-operable chest-room door (bits 10-25).
        /// </summary>
        private void SendCryptConfiguration() =>
            Character.Configurations.SendStandardConfiguration(452,
                (1 << (6 + _cryptStartIndex)) | (_puzzleSolved ? 0 : BarrowsPuzzle.LockedDoorMask(_puzzleDoorIndex)));

        /// <summary>
        ///     Restores the run from the character profile and registers the kill handler.
        /// </summary>
        private void LoadRun()
        {
            for (var i = 0; i < _spawnEntries.Length; i++)
            {
                _spawnEntries[i] = new SpawnEntry(BarrowsConstants.BarrowBrotherIDs[i], _npcBuilder, _hintIconBuilder);
            }

            var barrowsDto = Character.Profile.GetObject(BarrowsConstants.MinigamesBarrows, NewRun())!;

            if (barrowsDto.AhrimKilled)
            {
                _spawnEntries[BarrowsConstants.AhrimIndex].SetKilled();
            }

            if (barrowsDto.DharokKilled)
            {
                _spawnEntries[BarrowsConstants.DharokIndex].SetKilled();
            }

            if (barrowsDto.GuthanKilled)
            {
                _spawnEntries[BarrowsConstants.GuthanIndex].SetKilled();
            }

            if (barrowsDto.KarilKilled)
            {
                _spawnEntries[BarrowsConstants.KarilIndex].SetKilled();
            }

            if (barrowsDto.ToragKilled)
            {
                _spawnEntries[BarrowsConstants.ToragIndex].SetKilled();
            }

            if (barrowsDto.VeracKilled)
            {
                _spawnEntries[BarrowsConstants.VeracIndex].SetKilled();
            }

            if (barrowsDto.AkriseaKilled)
            {
                _spawnEntries[BarrowsConstants.AkrisaeIndex].SetKilled();
            }

            _killCount = barrowsDto.KillCount;
            _creatureLevels = barrowsDto.CreatureLevels;
            _lootedChest = barrowsDto.LootedChest;
            _cryptStartIndex = barrowsDto.CryptStartIndex;
            // Profiles written before the six-mound fix can hold index 6 (Akrisae); pick a valid mound and keep the rest.
            _tunnelIndex = barrowsDto.TunnelIndex is >= 0 and < BarrowsConstants.MoundCount ? barrowsDto.TunnelIndex : NewRun().TunnelIndex;
            _puzzleDoorIndex = barrowsDto.PuzzleDoorIndex;
            _puzzleIndex = barrowsDto.PuzzleIndex;
            _puzzleSolved = barrowsDto.PuzzleSolved;
            UpdateBarrowsProfile(); // persist a freshly randomized tunnel so it stays stable across exits

            if (_lootedChest)
            {
                Character.Configurations.SendSetCameraShake(12, 25, 12, 25, 4);
            }

            DrawAllKills();

            _barrowsKillHandler = Character.RegisterEventHandler(new EventHappened<CreatureKillEvent>(e =>
            {
                if (e.Victim is INpc npc)
                {
                    var compositeID = npc.Appearance.CompositeID;
                    foreach (var entry in _spawnEntries)
                    {
                        if (entry.NpcID == compositeID && !entry.Killed && ReferenceEquals(entry.Spawn?.Npc, npc)) // only this player's own brother
                        {
                            entry.SetKilled();
                            _killCount++;
                            UpdateBarrowsProfile();
                            DrawAllKills();
                            return true;
                        }
                    }

                    if (compositeID >= 2031 && compositeID <= 2037)
                    {
                        _killCount++;
                        _creatureLevels += BarrowsRewards.CreatureCombatLevel(compositeID);
                        UpdateBarrowsProfile();
                        RefreshCreaturesSlain();
                        return true; // The event is handled.
                    }
                }

                return false; // The event is not handled.
            }));
        }

        private void UpdateBarrowsProfile() =>
            Character.Profile.SetObject(BarrowsConstants.MinigamesBarrows,
                new BarrowsDto
                {
                    AhrimKilled = _spawnEntries[BarrowsConstants.AhrimIndex].Killed,
                    AkriseaKilled = _spawnEntries[BarrowsConstants.AkrisaeIndex].Killed,
                    DharokKilled = _spawnEntries[BarrowsConstants.DharokIndex].Killed,
                    GuthanKilled = _spawnEntries[BarrowsConstants.GuthanIndex].Killed,
                    KarilKilled = _spawnEntries[BarrowsConstants.KarilIndex].Killed,
                    VeracKilled = _spawnEntries[BarrowsConstants.VeracIndex].Killed,
                    ToragKilled = _spawnEntries[BarrowsConstants.ToragIndex].Killed,
                    CryptStartIndex = _cryptStartIndex,
                    KillCount = _killCount,
                    CreatureLevels = _creatureLevels,
                    LootedChest = _lootedChest,
                    TunnelIndex = _tunnelIndex,
                    PuzzleDoorIndex = _puzzleDoorIndex,
                    PuzzleIndex = _puzzleIndex,
                    PuzzleSolved = _puzzleSolved
                });

        /// <summary>
        ///     Determines whether the specified index is killed.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public bool IsKilled(int index) => _spawnEntries[index].Killed;

        /// <summary>
        ///     Spawns the barrow brother.
        /// </summary>
        /// <param name="npcID">The NPC Id.</param>
        /// <param name="chanceOnTunnel">if set to <c>true</c> [chance configuration tunnel].</param>
        public void SpawnBarrowBrother(int npcID, bool chanceOnTunnel = true)
        {
            var index = npcID >= 2025 && npcID <= 2030 ? npcID - 2025 : 6;
            if (index == BarrowsConstants.AkrisaeIndex)
            {
                // TODO(quest gate, later): unlock Akrisae once a persisted Ritual of the Mahjarrat completion source
                // (cache quest 194, progress varbit 9829) is readable from scripts. Until then his encounter stays
                // locked for everyone rather than being granted globally.
                Character.SendChatMessage("You found nothing.");
                return;
            }

            if (chanceOnTunnel && _tunnelIndex == index)
            {
                Character.Widgets.OpenDialogue(new TunnelDialogue(Character.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(),
                        this,
                        npcID),
                    true);
                return;
            }

            if (_spawnEntries[index].Spawn != null || _spawnEntries[index].Killed)
            {
                Character.SendChatMessage("You found nothing.");
                return;
            }

            _spawnEntries[index].SpawnNpc(Character);
        }

        /// <summary>
        ///     Opens the chest.
        /// </summary>
        /// <returns>
        ///     Chest opened
        /// </returns>
        public bool OpenChest()
        {
            if (Character.Combat.IsInCombat())
            {
                return false;
            }

            var entry = _spawnEntries[_tunnelIndex];
            if (!_lootedChest && !entry.Killed)
            {
                entry.SpawnNpc(Character);
                return false;
            }

            Character.AddState(new BarrowsOpenedChestState());
            Character.SendChatMessage("You lift open the massive chest...");
            UpdateBarrowsProfile();
            return true;
        }

        /// <summary>
        ///     Loots the chest.
        /// </summary>
        /// <param name="obj">The object.</param>
        public void LootChest(IGameObject obj)
        {
            if (obj.IsDisabled || Character.Combat.IsInCombat() || !Character.HasState<BarrowsOpenedChestState>())
            {
                return;
            }

            if (_lootedChest)
            {
                Character.SendChatMessage("You already looted this chest!");
                return;
            }

            // Resolve every table first so a missing one never leaves the run claimed without its rewards.
            var slain = Enumerable.Range(0, BarrowsConstants.MoundCount).Where(i => _spawnEntries[i].Killed).ToArray();
            var tables = BarrowsRewards.RollRewardTables(slain, _creatureLevels, RandomStatic.Generator.Next)
                .Select(id => _lootService.FindGameObjectLootTable(id).Result)
                .ToList();
            if (tables.Any(table => table == null))
            {
                return;
            }

            foreach (var table in tables)
            {
                Character.Inventory.TryAddLoot(Character, table!, out _);
            }

            Character.SendChatMessage("You loot the chest and the tomb begins to shake!");

            Character.Configurations.SendSetCameraShake(12, 25, 12, 25, 4);

            // Searching the chest is the reset trigger: kills clear and a new tunnel is chosen now. The claimed flag
            // stays until the player leaves, so the same run cannot be claimed twice.
            _lootedChest = true;
            _tunnelIndex = NewRun().TunnelIndex;

            ResetKills();
            UpdateBarrowsProfile();
        }

        /// <summary>
        ///     Whether the door object is one of the four doors around the chest.
        /// </summary>
        public bool IsChestDoor(int objectId) => BarrowsPuzzle.ChestDoorIndex(objectId) >= 0;

        /// <summary>
        ///     Handles a click on a chest-room door. Returns true when the door may be passed now; otherwise the puzzle opens
        ///     (and <paramref name="onSolved" /> runs after a correct answer) or the door stays locked.
        /// </summary>
        public bool TryPassChestDoor(int objectId, Action onSolved)
        {
            if (_puzzleSolved)
            {
                return true;
            }

            if (BarrowsPuzzle.ChestDoorIndex(objectId) != _puzzleDoorIndex)
            {
                return false; // locked door
            }

            var (choices, correctIndex) = BarrowsPuzzle.Choices(_puzzleIndex, RandomStatic.Generator.Next);
            var widget = Character.ServiceProvider.GetRequiredService<BarrowsPuzzleWidget>();
            widget.Barrows = this;
            widget.PuzzleIndex = _puzzleIndex;
            widget.Choices = choices;
            widget.CorrectIndex = correctIndex;
            _onPuzzleSolved = onSolved;
            Character.Widgets.OpenWidget(BarrowsPuzzle.InterfaceId, 0, widget, false);
            return false;
        }

        /// <summary>
        ///     Applies the answer: a correct one opens the way, a wrong one moves the operable door and picks a new puzzle.
        /// </summary>
        public void AnswerPuzzle(bool correct)
        {
            var onSolved = _onPuzzleSolved;
            _onPuzzleSolved = null;
            if (correct)
            {
                _puzzleSolved = true;
                SendCryptConfiguration();
                UpdateBarrowsProfile();
                onSolved?.Invoke();
                return;
            }

            // A wrong answer reshuffles the tunnels: the operable door moves and the exit rope ends up in another room.
            (_puzzleDoorIndex, _puzzleIndex) = BarrowsPuzzle.Reroll(_puzzleDoorIndex, RandomStatic.Generator.Next);
            _cryptStartIndex = BarrowsPuzzle.RerollEntry(_cryptStartIndex, BarrowsConstants.CryptCharacterSpawns.Length, RandomStatic.Generator.Next);
            SendCryptConfiguration();
            UpdateBarrowsProfile();
        }

        /// <summary>
        ///     Teleports the automatic crypts.
        /// </summary>
        public void TeleportToCrypts()
        {
            Character.Movement.Lock(true);
            Character.QueueAnimation(Animation.Create(827));
            var index = _cryptStartIndex = RandomStatic.Generator.Next(BarrowsConstants.CryptCharacterSpawns.Length);
            var spawn = BarrowsConstants.CryptCharacterSpawns[index].Clone();
            Character.QueueTask(new RsTask(() =>
                {
                    Character.Movement.Teleport(Teleport.Create(spawn));
                    Character.Movement.Unlock(false);
                },
                1));
            UpdateBarrowsProfile();
        }

        /// <summary>
        ///     Crypts the game object click performed.
        /// </summary>
        public bool CryptGameObjectClickPerformed()
        {
            if (_lootedChest)
            {
                return true;
            }

            if (RandomStatic.Generator.Next(0, 25) == 5)
            {
                _spawnEntries[_tunnelIndex].SpawnNpc(Character);
            }

            return false;
        }

        /// <summary>
        ///     Refreshes the creatures slain.
        /// </summary>
        private void RefreshCreaturesSlain() => Character.Configurations.SendBitConfiguration(464, _killCount);

        /// <summary>
        ///     Draws all kills.
        /// </summary>
        private void DrawAllKills()
        {
            var config = 0;
            // Config 453 bits 0-5 hold the six brother flags (varbits 457-462); bit 6 belongs to varbit 463, so Akrisae
            // has no flag here.
            for (var i = 0; i < BarrowsConstants.MoundCount; i++)
            {
                if (!_spawnEntries[i].Killed)
                {
                    continue;
                }

                config |= 1 << i;
            }

            // 742 cache layout: bits 0-5 brother flags, bit 16 chest open (varbit 1394), bits 17-31 kill count (varbit 464).
            if (Character.HasState<BarrowsOpenedChestState>())
            {
                config |= 1 << 16;
            }

            config |= _killCount << 17;
            Character.Configurations.SendStandardConfiguration(453, config);
        }

        /// <summary>
        ///     Unloads this instance.
        /// </summary>
        private void Unload()
        {
            foreach (var entry in _spawnEntries)
            {
                entry.Despawn();
            }

            if (_barrowsKillHandler != null)
            {
                Character.UnregisterEventHandler<CreatureKillEvent>(_barrowsKillHandler);
            }

            Character.Configurations.SendStandardConfiguration(453, 0); // reset the interface
            Character.RemoveState<BarrowsBetweenDoorsState>();
            Character.RemoveState<BarrowsOpenedChestState>();
        }

        /// <summary>
        ///     Resets the kills.
        /// </summary>
        private void ResetKills()
        {
            _killCount = 0;
            _creatureLevels = 0;
                foreach (var entry in _spawnEntries)
            {
                entry.Reset();
            }

            DrawAllKills();
        }

        /// <summary>
        ///     Tick's character.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (!InCrypts)
            {
                return;
            }

            _cryptTick++;
            if (_cryptTick % 30 == 0) // every 18 seconds
            {
                var drainedPrayerPoints = 80 + 10 * BrothersKilled;
                Character.Statistics.DrainPrayerPoints(drainedPrayerPoints);

                var inter = Character.Widgets.GetOpenWidget(24);
                if (inter != null)
                {
                    if (Character.Location.Z != 0)
                    {
                        var index = RandomStatic.Generator.Next(_spawnEntries.Length - 1); // need to add Akrisea head?
                        if (_spawnEntries[index].Killed)
                        {
                            var heads = BarrowsConstants.BarrowBrotherHeads[0];
                            var head = heads[index];
                            var child = RandomStatic.Generator.Next(8, 12);
                            inter.DrawModel(child, _itemRepository.FindItemDefinitionById(head).InterfaceModelId);
                            inter.SetAnimation(child, 2085);
                            Character.QueueTask(new RsTask(() =>
                                {
                                    inter.DrawModel(child, -1);
                                    inter.SetAnimation(child, -1);
                                },
                                5));
                        }
                    }
                    else
                    {
                        var heads = BarrowsConstants.BarrowBrotherHeads[1];
                        var head = heads[RandomStatic.Generator.Next(heads.Length)];
                        var child = RandomStatic.Generator.Next(8, 12);
                        inter.DrawModel(child, _itemRepository.FindItemDefinitionById(head).InterfaceModelId);
                        inter.SetAnimation(child, 2085);
                        Character.QueueTask(new RsTask(() =>
                            {
                                inter.DrawModel(child, -1);
                                inter.SetAnimation(child, -1);
                            },
                            5));
                    }
                }
            }

            if (!_lootedChest)
            {
                return;
            }

            if (!(RandomStatic.Generator.NextDouble() <= 0.05))
            {
                return;
            }

            var damage = -1;
            if (!Character.Combat.IsDead)
            {
                damage = RandomStatic.Generator.Next(30, 40);
                damage = Character.Statistics.DamageLifePoints(damage);
            }

            var splatBuilder = Character.ServiceProvider.GetRequiredService<IHitSplatBuilder>();
            var splat = splatBuilder.Create()
                .AddSprite(builder => builder.WithDamage(damage).WithSplatType(HitSplatType.HitSimpleDamage))
                .Build();
            Character.QueueHitSplat(splat);
            if (damage <= 0)
            {
                return;
            }

            Character.QueueGraphic(Graphic.Create(405));
            Character.QueueAnimation(Animation.Create(424));
        }

        /// <summary>
        ///     Death preserves the unclaimed run; living encounters are despawned. A brother that is already dead is left
        ///     alone so its delayed kill credit is still awarded.
        /// </summary>
        public override void OnDeath()
        {
            foreach (var entry in _spawnEntries)
            {
                if (entry.Spawn?.Npc.Combat.IsDead == true)
                {
                    continue;
                }

                entry.Despawn();
            }
        }

        /// <summary>
        ///     Get's called when character is destroyed permanently.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDestroy() => Unload();

        /// <summary>
        ///     Called when this script is removed from the character.
        ///     By default this method does nothing.
        /// </summary>
        public override void OnRemove()
        {
            Unload();

            Character.Configurations.SendResetCameraShake();

            if (_lootedChest)
            {
                // The chest was claimed: the next visit starts a fresh run. The tunnel was already rolled at the search.
                Character.Profile.SetObject(BarrowsConstants.MinigamesBarrows, NewRun() with { TunnelIndex = _tunnelIndex });
            }

            Character.SendChatMessage("The power of all those you slew in the barrows drains from your body.");
        }
    }
}
