using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Combat.Magic;
using Hagalaz.Game.Scripts.Skills.Magic.MiscSpells;
using Hagalaz.Game.Scripts.Skills.Magic.SkillSpells;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Scripts.Widgets.Lodestone;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Projectile;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents the spellbook tab.
    /// </summary>
    public class SpellBookTab : WidgetScript
    {
        /// <summary>
        ///     Contains normal combat spells.
        /// </summary>
        private static readonly Dictionary<int, ICombatSpell> _normalCombatSpells = new();

        /// <summary>
        ///     The Ancient spell families: the first spellbook button of each family and its spell factory. A family occupies four
        ///     consecutive buttons laid out as rush, blitz, burst, barrage, which are spell types 0, 2, 1 and 3.
        /// </summary>
        private static readonly (int FirstButtonId, Func<int, CombatSpellDto, IProjectileBuilder, ICombatSpell> Create)[] _ancientFamilies =
        [
            (20, (type, dto, builder) => new AncientIceCombatSpell(type, dto, builder)),
            (24, (type, dto, builder) => new AncientBloodCombatSpell(type, dto, builder)),
            (28, (type, dto, builder) => new AncientSmokeCombatSpell(type, dto, builder)),
            (32, (type, dto, builder) => new AncientShadowCombatSpell(type, dto, builder)),
            (36, (type, dto, builder) => new AncientMiasmicCombatSpell(type, dto, builder))
        ];

        private static readonly int[] _ancientSpellTypeByButtonOffset = [0, 2, 1, 3];

        /// <summary>
        ///     Builds the Ancient combat spells from their persisted definitions, keyed by spellbook button id.
        ///     A button without a persisted definition is not castable.
        /// </summary>
        internal Dictionary<int, ICombatSpell> CreateAncientSpells(IEnumerable<CombatSpellDto> definitions)
        {
            var byButton = definitions.Where(definition => definition.SpellBook == MagicBook.AncientBook).ToDictionary(definition => definition.ButtonId);
            var spells = new Dictionary<int, ICombatSpell>();
            foreach (var (firstButtonId, create) in _ancientFamilies)
            {
                for (var offset = 0; offset < _ancientSpellTypeByButtonOffset.Length; offset++)
                {
                    if (byButton.TryGetValue(firstButtonId + offset, out var definition))
                    {
                        spells.Add(definition.ButtonId, create(_ancientSpellTypeByButtonOffset[offset], definition, _projectileBuilder));
                    }
                }
            }

            return spells;
        }

        private static readonly (Dictionary<int, ITeleportSpellScript> Normal, Dictionary<int, ITeleportSpellScript> Ancient, Dictionary<int, ITeleportSpellScript> Lunar) _teleports = CreateTeleports();

        /// <summary>
        ///     Contains normal teleports.
        /// </summary>
        private static Dictionary<int, ITeleportSpellScript> _normalTeleports => _teleports.Normal;

        /// <summary>
        ///     Contains ancient teleports.
        /// </summary>
        private static Dictionary<int, ITeleportSpellScript> _ancientTeleports => _teleports.Ancient;

        /// <summary>
        ///     Contains lunar teleports.
        /// </summary>
        private static Dictionary<int, ITeleportSpellScript> _lunarTeleports => _teleports.Lunar;

        private readonly IScopedGameMediator _gameMediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMagicService _magicService;
        private readonly ICombatSpellCatalog _combatSpellCatalog;
        private readonly IItemBuilder _itemBuilder;
        private readonly IProjectileBuilder _projectileBuilder;

        private static (Dictionary<int, ITeleportSpellScript> Normal, Dictionary<int, ITeleportSpellScript> Ancient, Dictionary<int, ITeleportSpellScript> Lunar) CreateTeleports()
        {
            var normalTeleports = new Dictionary<int, ITeleportSpellScript>();
            var ancientTeleports = new Dictionary<int, ITeleportSpellScript>();
            var lunarTeleports = new Dictionary<int, ITeleportSpellScript>();
            normalTeleports.Add(37,
                new StandardTeleportScript(MagicBook.StandardBook,
                    2413,
                    2840,
                    0,
                    5,
                    10,
                    19,
                    [RuneType.Law, RuneType.Water, RuneType.Air],
                    [
                        1, 1, 1
                    ])); // Mobilising armies teleport
            normalTeleports.Add(40,
                new StandardTeleportScript(MagicBook.StandardBook,
                    3213,
                    3428,
                    0,
                    5,
                    25,
                    35,
                    [RuneType.Law, RuneType.Fire, RuneType.Air],
                    [
                        1, 1, 3
                    ])); // Varrock teleport
            normalTeleports.Add(43,
                new StandardTeleportScript(MagicBook.StandardBook,
                    3222,
                    3218,
                    0,
                    4,
                    31,
                    41,
                    [RuneType.Law, RuneType.Earth, RuneType.Air],
                    [
                        1, 1, 3
                    ])); // Lumbridge teleport
            normalTeleports.Add(46,
                new StandardTeleportScript(MagicBook.StandardBook,
                    2965,
                    3380,
                    0,
                    4,
                    37,
                    48,
                    [RuneType.Law, RuneType.Water, RuneType.Air],
                    [
                        1, 1, 3
                    ])); // Falador teleport
            normalTeleports.Add(51,
                new StandardTeleportScript(MagicBook.StandardBook, 2757, 3478, 0, 3, 45, 55.5, [RuneType.Law, RuneType.Air], [1, 5])); // Camelot teleport
            normalTeleports.Add(57,
                new StandardTeleportScript(MagicBook.StandardBook, 2662, 3307, 0, 6, 51, 61, [RuneType.Law, RuneType.Water], [2, 2])); // Ardougne teleport
            normalTeleports.Add(62,
                new StandardTeleportScript(MagicBook.StandardBook, 2931, 4711, 0, 3, 58, 68, [RuneType.Law, RuneType.Earth], [2, 2])); // Watchtower teleport
            normalTeleports.Add(69,
                new StandardTeleportScript(MagicBook.StandardBook, 2891, 3678, 0, 3, 61, 71, [RuneType.Law, RuneType.Fire], [2, 2])); // Trollheim teleport

            ancientTeleports.Add(40,
                new StandardTeleportScript(MagicBook.AncientBook,
                    3097,
                    9869,
                    0,
                    1,
                    54,
                    64,
                    [RuneType.Law, RuneType.Fire, RuneType.Air],
                    [
                        2, 1, 1
                    ])); // Paddewwa teleport
            ancientTeleports.Add(41,
                new StandardTeleportScript(MagicBook.AncientBook, 3308, 3332, 0, 1, 60, 70, [RuneType.Law, RuneType.Soul], [2, 1])); // Senntisten teleport
            ancientTeleports.Add(42,
                new StandardTeleportScript(MagicBook.AncientBook, 3494, 3477, 0, 2, 66, 76, [RuneType.Law, RuneType.Blood], [2, 1])); // Kharyrll  teleport
            ancientTeleports.Add(43,
                new StandardTeleportScript(MagicBook.AncientBook, 3003, 3470, 0, 2, 72, 82, [RuneType.Law, RuneType.Water], [2, 4])); // Lassar  teleport
            ancientTeleports.Add(44,
                new StandardTeleportScript(MagicBook.AncientBook,
                    2966,
                    3696,
                    0,
                    2,
                    78,
                    88,
                    [RuneType.Law, RuneType.Fire, RuneType.Air],
                    [
                        2, 3, 2
                    ])); // Dareeyak  teleport
            ancientTeleports.Add(45,
                new StandardTeleportScript(MagicBook.AncientBook, 3185, 3664, 0, 3, 84, 94, [RuneType.Law, RuneType.Soul], [2, 2])); // Carrallangar  teleport
            ancientTeleports.Add(46,
                new StandardTeleportScript(MagicBook.AncientBook, 3287, 3883, 0, 2, 90, 100, [RuneType.Law, RuneType.Blood], [2, 2])); // Annakarl  teleport
            ancientTeleports.Add(47,
                new StandardTeleportScript(MagicBook.AncientBook, 2972, 3873, 0, 2, 96, 106, [RuneType.Law, RuneType.Water], [2, 8])); // Ghorrock  teleport

            lunarTeleports.Add(43,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2110,
                    3915,
                    0,
                    2,
                    69,
                    66,
                    [RuneType.Astral, RuneType.Law, RuneType.Earth],
                    [
                        2, 1, 2
                    ])); // Moonclan teleport
            lunarTeleports.Add(54,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2455,
                    3234,
                    0,
                    1,
                    71,
                    69,
                    [RuneType.Astral, RuneType.Law, RuneType.Earth],
                    [
                        2, 1, 6
                    ])); // Ourania teleport
            lunarTeleports.Add(67,
                new StandardTeleportScript(MagicBook.LunarBook,
                    3007,
                    3321,
                    0,
                    2,
                    72,
                    70,
                    [RuneType.Astral, RuneType.Law, RuneType.Air],
                    [
                        2, 1, 2
                    ])); // South Falador teleport
            lunarTeleports.Add(47,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2541,
                    3760,
                    0,
                    2,
                    72,
                    71,
                    [RuneType.Astral, RuneType.Law, RuneType.Water],
                    [
                        2, 1, 1
                    ])); // Waterbirth teleport
            lunarTeleports.Add(22,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2519,
                    3571,
                    0,
                    2,
                    75,
                    76,
                    [RuneType.Astral, RuneType.Law, RuneType.Fire],
                    [
                        2, 2, 3
                    ])); // Barbarian teleport
            lunarTeleports.Add(69,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2636,
                    3340,
                    0,
                    2,
                    76,
                    76,
                    [RuneType.Astral, RuneType.Law, RuneType.Water],
                    [
                        2, 1, 5
                    ])); // North Ardougne teleport
            lunarTeleports.Add(41,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2660,
                    3157,
                    0,
                    2,
                    78,
                    80,
                    [RuneType.Astral, RuneType.Law, RuneType.Water],
                    [
                        2, 2, 4
                    ])); // Khazard teleport
            lunarTeleports.Add(40,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2613,
                    3381,
                    0,
                    2,
                    85,
                    89,
                    [RuneType.Astral, RuneType.Law, RuneType.Water],
                    [
                        3, 3, 8
                    ])); // Fishing guild teleport
            lunarTeleports.Add(44,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2805,
                    3434,
                    0,
                    2,
                    87,
                    92,
                    [RuneType.Astral, RuneType.Law, RuneType.Water],
                    [
                        3, 3, 10
                    ])); // Catherby teleport
            lunarTeleports.Add(51,
                new StandardTeleportScript(MagicBook.LunarBook,
                    2952,
                    3931,
                    0,
                    2,
                    89,
                    96,
                    [RuneType.Astral, RuneType.Law, RuneType.Water],
                    [
                        3, 3, 8
                    ])); // Ice plateau teleport
            return (normalTeleports, ancientTeleports, lunarTeleports);
        }

        private IGameConnectHandle _settingsChanged = default!;
        private IGameConnectHandle _bookChanged = default!;

        public SpellBookTab(
            ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator, IServiceProvider serviceProvider,
            IMagicService magicService, ICombatSpellCatalog combatSpellCatalog, IItemBuilder itemBuilder, IProjectileBuilder projectileBuilder) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
            _serviceProvider = serviceProvider;
            _magicService = magicService;
            _combatSpellCatalog = combatSpellCatalog;
            _itemBuilder = itemBuilder;
            _projectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _settingsChanged = _gameMediator.ConnectHandler<ProfileValueChanged<bool>>((context) =>
            {
                if (context.Message.Key == ProfileConstants.CombatSettingsMagicDefensiveCasting)
                {
                    RefreshMagicBook();
                }

                if (context.Message.Key == ProfileConstants.MagicSettingsHideCombatSpells ||
                    context.Message.Key == ProfileConstants.MagicSettingsHideMiscSpells ||
                    context.Message.Key == ProfileConstants.MagicSettingsHideSkillSpells ||
                    context.Message.Key == ProfileConstants.MagicSettingsHideTeleportSpells)
                {
                    RefreshMagicFilters();
                }
            });
            _bookChanged = _gameMediator.ConnectHandler<ProfileValueChanged<MagicBook>>((context) =>
            {
                Owner.Magic.ClearAutoCastingSpell(false);
                return Task.CompletedTask;
            });

            var magicManager = _magicService;

            switch (Owner.Profile.GetValue<MagicBook>(ProfileConstants.MagicSettingsBook, MagicBook.StandardBook))
            {
                case MagicBook.StandardBook:
                    {
                        InterfaceInstance.AttachClickHandler(24,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                Owner.Widgets.OpenWidget(1092, 0, _serviceProvider.GetRequiredService<LodestoneInterface>(), true);
                                return true;
                            });
                        InterfaceInstance.AttachClickHandler(2,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.CombatSettingsMagicDefensiveCasting));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(7,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsHideCombatSpells));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(9,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsHideTeleportSpells));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(11,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsHideMiscSpells));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(13,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsHideSkillSpells));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(27,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                var enchantedCrossbowWindow = Owner.ServiceProvider.GetRequiredService<EnchantCrossbowBoltWindow>();
                                Owner.Widgets.OpenWidget(432, 0, enchantedCrossbowWindow, true);
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(33,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                var bonesToBananas = _serviceProvider.GetRequiredService<IBonesToBananas>();
                                return bonesToBananas.Cast();
                            });

                        InterfaceInstance.AttachUseOnComponentHandler(38,
                            (componentID, extraInfo1, extraInfo2, itemUsedOnId, itemUsedOnSlot) =>
                            {
                                if (itemUsedOnSlot < 0 || itemUsedOnSlot >= Owner.Inventory.Items.Capacity)
                                {
                                    return false;
                                }

                                var item = Owner.Inventory.Items[itemUsedOnSlot];
                                if (item == null || item.Id != itemUsedOnId)
                                {
                                    return false;
                                }

                                var lowLevelAlchemy = _serviceProvider.GetRequiredService<ILowLevelAlchemy>();
                                return lowLevelAlchemy.Cast(item);
                            });

                        InterfaceInstance.AttachUseOnComponentHandler(59,
                            (componentID, extraInfo1, extraInfo2, itemUsedOnId, itemUsedOnSlot) =>
                            {
                                if (itemUsedOnSlot < 0 || itemUsedOnSlot >= Owner.Inventory.Items.Capacity)
                                {
                                    return false;
                                }

                                var item = Owner.Inventory.Items[itemUsedOnSlot];
                                if (item == null || item.Id != itemUsedOnId)
                                {
                                    return false;
                                }

                                var highLevelAlchemy = _serviceProvider.GetRequiredService<IHighLevelAlchemy>();
                                return highLevelAlchemy.Cast(item);
                            });

                        InterfaceInstance.AttachClickHandler(65,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                var bonesToPeaches = _serviceProvider.GetRequiredService<IBonesToPeaches>();
                                return bonesToPeaches.Cast();
                            });

                        InterfaceInstance.AttachClickHandler(83,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                if (Owner.HasState<CantCastChargeState>())
                                {
                                    Owner.SendChatMessage("You can only cast charge once every minute.");
                                }
                                else
                                {
                                    if (CheckMagicLevel(80, false) && Owner.Magic.CheckRunes([RuneType.Fire, RuneType.Blood, RuneType.Air], [3, 3, 3]))
                                    {
                                        Owner.Magic.RemoveRunes([RuneType.Fire, RuneType.Blood, RuneType.Air], [3, 3, 3]);
                                        Owner.Statistics.AddExperience(StatisticsConstants.Magic, 180);
                                        Owner.QueueAnimation(Animation.Create(811));
                                        //this.owner.QueueGraphic(Graphic.Create(726)); // TODO graphic id
                                        Owner.AddState(new CantCastChargeState { TicksLeft = 100 }); // 1 minute
                                        Owner.AddState(new ChargeState { TicksLeft = 700 }); // 7 minutes
                                    }
                                }

                                return true;
                            });

                        foreach (var childID in _normalTeleports.Keys)
                        {
                            InterfaceInstance.AttachClickHandler(childID,
                                (componentID, clickType, extra1, extra2) =>
                                {
                                    if (clickType != ComponentClickType.LeftClick)
                                    {
                                        return false;
                                    }

                                    Cast(_normalTeleports[componentID]);
                                    return true;
                                });
                        }

                        foreach (var definition in _combatSpellCatalog.CombatSpells
                                     .Where(spell => spell.SpellBook == MagicBook.StandardBook))
                        {
                            InterfaceInstance.AttachClickHandler(definition.ButtonId,
                                (componentID, clickType, extra1, extra2) =>
                                {
                                    if (clickType != ComponentClickType.LeftClick)
                                    {
                                        return false;
                                    }

                                    AutoCast(_normalCombatSpells.TryGetValue(definition.ButtonId, out var spell) ? spell : new StandardCombatSpell(definition));
                                    return true;
                                });

                            InterfaceInstance.AttachUseOnCreatureHandler(definition.ButtonId,
                                (componentID, usedOn, shouldRun, extra1, extra2) =>
                                {
                                    Owner.Interrupt(this);
                                    Cast(_normalCombatSpells.TryGetValue(definition.ButtonId, out var spell) ? spell : new StandardCombatSpell(definition),
                                        usedOn,
                                        shouldRun);
                                    return true;
                                });
                        }

                        foreach (var definition in magicManager.FindAllEnchantingSpells().ConfigureAwait(false).GetAwaiter().GetResult())
                        {
                            InterfaceInstance.AttachUseOnComponentHandler(definition.ButtonId,
                                (componentID, extraInfo1, extraInfo2, itemUsedOnId, itemUsedOnSlot) =>
                                {
                                    if (itemUsedOnSlot < 0 || itemUsedOnSlot >= Owner.Inventory.Items.Capacity)
                                    {
                                        return false;
                                    }

                                    var item = Owner.Inventory.Items[itemUsedOnSlot];
                                    if (item == null || item.Id != itemUsedOnId)
                                    {
                                        return false;
                                    }

                                    Owner.QueueTask(cancellationToken => CastEnchant(Owner, item, definition, cancellationToken));
                                    return true;
                                });
                        }

                        break;
                    }
                case MagicBook.LunarBook:
                    {
                        InterfaceInstance.AttachClickHandler(39,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                Owner.Widgets.OpenWidget(1092, 0, _serviceProvider.GetRequiredService<LodestoneInterface>(), true);
                                return true;
                            });
                        InterfaceInstance.AttachClickHandler(5,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsHideCombatSpells));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(7,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsHideTeleportSpells));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(9,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsHideMiscSpells));
                                return true;
                            });

                        InterfaceInstance.AttachClickHandler(20,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.CombatSettingsMagicDefensiveCasting));
                                return true;
                            });

                        foreach (var childID in _lunarTeleports.Keys)
                        {
                            InterfaceInstance.AttachClickHandler(childID,
                                (componentID, clickType, extra1, extra2) =>
                                {
                                    if (clickType != ComponentClickType.LeftClick)
                                    {
                                        return false;
                                    }

                                    Cast(_lunarTeleports[componentID]);
                                    return true;
                                });
                        }

                        OnComponentClick spellClickHandler = (componentID, clickType, extra1, extra2) =>
                        {
                            if (clickType != ComponentClickType.LeftClick)
                            {
                                return false;
                            }

                            if (componentID == 37) // vengeance
                            {
                                if (Owner.HasState<CantCastVengeanceState>())
                                {
                                    Owner.SendChatMessage("You can only cast vengeance spells once every 30 seconds.");
                                }
                                else
                                {
                                    if (CheckMagicLevel(94, false) && Owner.Magic.CheckRunes([RuneType.Astral, RuneType.Death, RuneType.Earth], [4, 2, 10]))
                                    {
                                        Owner.Magic.RemoveRunes([RuneType.Astral, RuneType.Death, RuneType.Earth], [4, 2, 10]);
                                        Owner.Statistics.AddExperience(StatisticsConstants.Magic, 112.0);
                                        Owner.QueueAnimation(Animation.Create(4410));
                                        Owner.QueueGraphic(Graphic.Create(726));
                                        Owner.AddState(new CantCastVengeanceState { TicksLeft = 50 });
                                        Owner.AddState(new VengeanceState { TicksLeft = 500 });
                                    }
                                }
                            }

                            return true;
                        };
                        InterfaceInstance.AttachClickHandler(37, spellClickHandler); // vengeance
                        break;
                    }
                case MagicBook.AncientBook:
                    {
                        InterfaceInstance.AttachClickHandler(48,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }

                                Owner.Widgets.OpenWidget(1092, 0, _serviceProvider.GetRequiredService<LodestoneInterface>(), true);
                                return true;
                            });

                        AttachToggleHandler(5, ProfileConstants.MagicSettingsHideCombatSpells);
                        AttachToggleHandler(7, ProfileConstants.MagicSettingsHideTeleportSpells);
                        AttachToggleHandler(18, ProfileConstants.CombatSettingsMagicDefensiveCasting);

                        foreach (var childID in _ancientTeleports.Keys)
                        {
                            InterfaceInstance.AttachClickHandler(childID,
                                (componentID, clickType, extra1, extra2) =>
                                {
                                    if (clickType != ComponentClickType.LeftClick)
                                    {
                                        return false;
                                    }

                                    Cast(_ancientTeleports[componentID]);
                                    return true;
                                });
                        }

                        var ancientSpells = CreateAncientSpells(_combatSpellCatalog.CombatSpells);
                        foreach (var (buttonId, spell) in ancientSpells)
                        {
                            InterfaceInstance.AttachClickHandler(buttonId,
                                (componentID, clickType, extra1, extra2) =>
                                {
                                    if (clickType != ComponentClickType.Option6Click)
                                    {
                                        return false;
                                    }

                                    AutoCast(spell);
                                    return true;
                                });

                            InterfaceInstance.AttachUseOnCreatureHandler(buttonId,
                                (componentID, usedOn, shouldRun, extra1, extra2) =>
                                {
                                    Owner.Interrupt(this);
                                    Cast(spell, usedOn, shouldRun);
                                    return true;
                                });
                        }

                        break;
                    }
            }

            Refresh();
        }

        /// <summary>
        ///     Attaches a left-click handler that toggles a boolean profile setting.
        /// </summary>
        private void AttachToggleHandler(int componentId, string profileKey) =>
            InterfaceInstance.AttachClickHandler(componentId,
                (componentID, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    _gameMediator.Publish(new ProfileToggleBoolAction(profileKey));
                    return true;
                });

        public async Task<bool> CastEnchant(
            ICharacter character,
            IItem item,
            EnchantingSpellDto dto,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (!character.Magic.CheckMagicLevel(dto.RequiredLevel))
            {
                return false;
            }

            if (!character.Magic.CheckRunes(dto.RequiredRunes, dto.RequiredRunesCounts))
            {
                return false;
            }

            var product = await _magicService.FindEnchantingSpellProductByButtonId(dto.ButtonId);
            cancellationToken.ThrowIfCancellationRequested();
            if (product == null)
            {
                character.SendChatMessage("You can not enchant this item!");
                return false;
            }

            var slot = character.Inventory.Items.GetInstanceSlot(item);
            if (slot == -1)
            {
                return false;
            }

            if (!character.Magic.CheckMagicLevel(dto.RequiredLevel) ||
                !character.Magic.CheckRunes(dto.RequiredRunes, dto.RequiredRunesCounts))
            {
                return false;
            }

            character.Magic.RemoveRunes(dto.RequiredRunes, dto.RequiredRunesCounts);
            character.Inventory.ReplaceItem(slot, _itemBuilder.Create().WithId(product.ProductId).Build());
            // TODO - Animation
            character.QueueGraphic(Graphic.Create(dto.GraphicId));
            character.Statistics.AddExperience(StatisticsConstants.Magic, dto.Experience);
            return true;
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            _settingsChanged?.Disconnect();
            _bookChanged?.Disconnect();
        }

        public void Refresh()
        {
            RefreshMagicBook();
            RefreshMagicFilters();
        }

        /// <summary>
        /// Refreshes the defensive casting.
        /// </summary>
        public void RefreshMagicBook()
        {
            var spellBookId = 0;
            var book = Owner.Profile.GetValue<MagicBook>(ProfileConstants.MagicSettingsBook);
            spellBookId = book switch
            {
                MagicBook.AncientBook => 1,
                MagicBook.LunarBook => 2,
                MagicBook.DungeoneeringBook => 3,
                _ => spellBookId
            };

            Owner.Configurations.SendStandardConfiguration(439,
                spellBookId | (Owner.Profile.GetValue<bool>(ProfileConstants.CombatSettingsMagicDefensiveCasting) ? 1 << 8 : 0));
        }

        /// <summary>
        /// Refreshes the magic filters.
        /// </summary>
        public void RefreshMagicFilters()
        {
            var hash = 0;
            var book = Owner.Profile.GetValue<MagicBook>(ProfileConstants.MagicSettingsBook);
            switch (book)
            {
                case MagicBook.StandardBook:
                    hash = 0 | (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideCombatSpells) ? 1 : 0) << 9;
                    hash |= (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideSkillSpells) ? 1 : 0) << 10;
                    hash |= (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideMiscSpells) ? 1 : 0) << 11;
                    hash |= (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideTeleportSpells) ? 1 : 0) << 12;
                    break;
                case MagicBook.AncientBook:
                    hash = 1 << 3 | (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideCombatSpells) ? 1 : 0) << 16;
                    hash |= (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideTeleportSpells) ? 1 : 0) << 17;
                    break;
                case MagicBook.LunarBook:
                    hash = 2 << 6 | (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideCombatSpells) ? 1 : 0) << 13;
                    hash |= (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideMiscSpells) ? 1 : 0) << 14;
                    hash |= (Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsHideTeleportSpells) ? 1 : 0) << 15;
                    break;
            }

            Owner.Configurations.SendStandardConfiguration(1376, hash);
        }

        /// <summary>
        ///     Check's magic level , does send your magic level is not enough to cast this spell
        ///     if level doesn't fit requirements.
        /// </summary>
        /// <returns></returns>
        private bool CheckMagicLevel(int required, bool combatSpell)
        {
            var baseLevel = Owner.Statistics.LevelForExperience(StatisticsConstants.Magic);
            var level = Owner.Statistics.GetSkillLevel(StatisticsConstants.Magic);

            if (level < required && (!combatSpell || baseLevel < required))
            {
                Owner.SendChatMessage("Your magic level is not high enough for this spell.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Autocast's specific spell.
        /// </summary>
        private void AutoCast(ICombatSpell spell)
        {
            if (!spell.CheckRequirements(Owner))
            {
                return;
            }

            if (Owner.Magic.AutoCastingSpell == spell)
            {
                Owner.Magic.ClearAutoCastingSpell();
            }
            else
            {
                Owner.Magic.SetAutoCastingSpell(spell);
            }
        }

        /// <summary>
        ///     Cast's standard spell.
        /// </summary>
        private void Cast(ICombatSpell spell, ICreature target, bool forceRun)
        {
            if (!spell.CanAttack(Owner, target))
            {
                return;
            }

            if (!spell.CheckRequirements(Owner))
            {
                return;
            }

            Owner.Magic.SelectedSpell = spell;
            Owner.ForceRunMovementType(forceRun);
            Owner.FaceLocation(target);
            Owner.Combat.SetTarget(target.Handle);
        }

        /// <summary>
        ///     Casts the specified spell.
        /// </summary>
        /// <param name="spellScript">The spell.</param>
        private void Cast(ITeleportSpellScript spellScript)
        {
            if (!spellScript.CanTeleport(Owner))
            {
                return;
            }

            spellScript.PerformTeleport(Owner);
        }
    }
}
