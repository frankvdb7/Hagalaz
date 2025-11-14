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
            npc.QueueTask(new RsTask(() => npc.Combat.SetTarget(character), 1));

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
                    Character.Configurations.SendStandardConfiguration(452, (1 + _cryptStartIndex) << 6); // randomize dungeon (with rope)
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

        /// <summary>
        ///     The barrow brother kill count.
        /// </summary>
        private int _barrowBrotherKillCount;

        /// <summary>
        ///     The barrows kill handler.
        /// </summary>
        private EventHappened? _barrowsKillHandler;

        /// <summary>
        ///     The barrow brother index of which the sarcophagus may lead to the tunnel.
        /// </summary>
        private int _tunnelIndex;

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
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            for (var i = 0; i < _spawnEntries.Length; i++)
            {
                _spawnEntries[i] = new SpawnEntry(BarrowsConstants.BarrowBrotherIDs[i], _npcBuilder, _hintIconBuilder);
            }

            var barrowsDto = Character.Profile.GetObject(BarrowsConstants.MinigamesBarrows,
                new BarrowsDto
                {
                    TunnelIndex = RandomStatic.Generator.Next(0, _spawnEntries.Length)
                });

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
            _lootedChest = barrowsDto.LootedChest;
            _cryptStartIndex = barrowsDto.CryptStartIndex;
            _tunnelIndex = barrowsDto.TunnelIndex;

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
                        if (entry.NpcID == compositeID && !entry.Killed)
                        {
                            entry.SetKilled();
                            _barrowBrotherKillCount++;
                            _killCount++;
                            UpdateBarrowsProfile();
                            DrawAllKills();
                            return true;
                        }
                    }

                    if (compositeID >= 2031 && compositeID <= 2037)
                    {
                        _killCount++;
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
                    LootedChest = _lootedChest,
                    TunnelIndex = _tunnelIndex
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
            if (!entry.Killed)
            {
                entry.SpawnNpc(Character);
                return false;
            }

            Character.AddState(new BarrowsOpenedChestState { TicksLeft = int.MaxValue });
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
            if (obj.IsDestroyed || Character.Combat.IsInCombat() || !Character.HasState<BarrowsOpenedChestState>())
            {
                return;
            }

            if (_lootedChest)
            {
                Character.SendChatMessage("You already looted this chest!");
                return;
            }

            var table = _lootService.FindGameObjectLootTable(obj.Definition.LootTableId).Result;
            if (table == null)
            {
                return;
            }

            Character.Inventory.TryAddLoot(Character, table, out _);

            Character.SendChatMessage("You loot the chest and the tomb begins to shake!");

            Character.Configurations.SendSetCameraShake(12, 25, 12, 25, 4);

            _lootedChest = true;

            ResetKills();
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
            for (var i = 0; i < _spawnEntries.Length; i++)
            {
                if (!_spawnEntries[i].Killed)
                {
                    continue;
                }

                config |= 1 << i;
            }

            config = (_killCount << 1 << 16) | config;
            config = ((Character.HasState<BarrowsOpenedChestState>() ? 1 : 0) << 1 << 16) | config;
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
        }

        /// <summary>
        ///     Resets the kills.
        /// </summary>
        private void ResetKills()
        {
            _killCount = 0;
            _barrowBrotherKillCount = 0;
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
                var drainedPrayerPoints = 80 + 10 * _barrowBrotherKillCount;
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

            Character.Profile.SetObject(BarrowsConstants.MinigamesBarrows, new BarrowsDto());

            Character.Configurations.SendResetCameraShake();

            if (!_lootedChest)
            {
                Character.SendChatMessage("The power of all those you slew in the barrows drains from your body.");
            }
        }
    }
}