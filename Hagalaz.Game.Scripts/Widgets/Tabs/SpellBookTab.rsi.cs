using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Configuration;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Features.States;
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
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Combat.Magic;
using Hagalaz.Game.Scripts.Skills.Magic.MiscSpells;
using Hagalaz.Game.Scripts.Skills.Magic.SkillSpells;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;
using Hagalaz.Game.Scripts.Widgets.Lodestone;
using Hagalaz.Game.Abstractions.Builders.Item;

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
        ///     Initializes the <see cref="SpellBookTab" /> class.
        /// </summary>
        static SpellBookTab() => LoadTeleports();

        // TODO - this is not good
        //_ =LoadScriptedCombatSpells();
        /// <summary>
        ///     Contains smoke spells.
        /// </summary>
        private static readonly ICombatSpell[] _smokeSpells =
        [
            new AncientSmokeCombatSpell(0), new AncientSmokeCombatSpell(1), new AncientSmokeCombatSpell(2), new AncientSmokeCombatSpell(3)
        ];

        /// <summary>
        ///     Contains shadow spells.
        /// </summary>
        private static readonly ICombatSpell[] _shadowSpells =
        [
            new AncientShadowCombatSpell(0), new AncientShadowCombatSpell(1), new AncientShadowCombatSpell(2), new AncientShadowCombatSpell(3)
        ];

        /// <summary>
        ///     Contains blood spells.
        /// </summary>
        private static readonly ICombatSpell[] _bloodSpells =
        [
            new AncientBloodCombatSpell(0), new AncientBloodCombatSpell(1), new AncientBloodCombatSpell(2), new AncientBloodCombatSpell(3)
        ];

        /// <summary>
        ///     Contains ice spells.
        /// </summary>
        private static readonly ICombatSpell[] _iceSpells =
        [
            new AncientIceCombatSpell(0), new AncientIceCombatSpell(1), new AncientIceCombatSpell(2), new AncientIceCombatSpell(3)
        ];

        /// <summary>
        ///     Contains miasmic spells.
        /// </summary>
        private static readonly ICombatSpell[] _miasmicSpells =
        [
            new AncientMiasmicCombatSpell(0), new AncientMiasmicCombatSpell(1), new AncientMiasmicCombatSpell(2), new AncientMiasmicCombatSpell(3)
        ];

        /// <summary>
        ///     Contains normal teleports.
        /// </summary>
        private static readonly Dictionary<int, ITeleportSpellScript> _normalTeleports = new();

        /// <summary>
        ///     Contains ancient teleports.
        /// </summary>
        private static readonly Dictionary<int, ITeleportSpellScript> _ancientTeleports = new();

        /// <summary>
        ///     Contains lunar teleports.
        /// </summary>
        private static readonly Dictionary<int, ITeleportSpellScript> _lunarTeleports = new();

        private readonly IScopedGameMediator _gameMediator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMagicService _magicService;
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     Loads the teleports.
        /// </summary>
        private static void LoadTeleports()
        {
            _normalTeleports.Add(37,
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
            _normalTeleports.Add(40,
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
            _normalTeleports.Add(43,
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
            _normalTeleports.Add(46,
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
            _normalTeleports.Add(51,
                new StandardTeleportScript(MagicBook.StandardBook, 2757, 3478, 0, 3, 45, 55.5, [RuneType.Law, RuneType.Air], [1, 5])); // Camelot teleport
            _normalTeleports.Add(57,
                new StandardTeleportScript(MagicBook.StandardBook, 2662, 3307, 0, 6, 51, 61, [RuneType.Law, RuneType.Water], [2, 2])); // Ardougne teleport
            _normalTeleports.Add(62,
                new StandardTeleportScript(MagicBook.StandardBook, 2931, 4711, 0, 3, 58, 68, [RuneType.Law, RuneType.Earth], [2, 2])); // Watchtower teleport
            _normalTeleports.Add(69,
                new StandardTeleportScript(MagicBook.StandardBook, 2891, 3678, 0, 3, 61, 71, [RuneType.Law, RuneType.Fire], [2, 2])); // Trollheim teleport

            _ancientTeleports.Add(40,
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
            _ancientTeleports.Add(41,
                new StandardTeleportScript(MagicBook.AncientBook, 3308, 3332, 0, 1, 60, 70, [RuneType.Law, RuneType.Soul], [2, 1])); // Senntisten teleport
            _ancientTeleports.Add(42,
                new StandardTeleportScript(MagicBook.AncientBook, 3494, 3477, 0, 2, 66, 76, [RuneType.Law, RuneType.Blood], [2, 1])); // Kharyrll  teleport
            _ancientTeleports.Add(43,
                new StandardTeleportScript(MagicBook.AncientBook, 3003, 3470, 0, 2, 72, 82, [RuneType.Law, RuneType.Water], [2, 4])); // Lassar  teleport
            _ancientTeleports.Add(44,
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
            _ancientTeleports.Add(45,
                new StandardTeleportScript(MagicBook.AncientBook, 3185, 3664, 0, 3, 84, 94, [RuneType.Law, RuneType.Soul], [2, 2])); // Carrallangar  teleport
            _ancientTeleports.Add(46,
                new StandardTeleportScript(MagicBook.AncientBook, 3287, 3883, 0, 2, 90, 100, [RuneType.Law, RuneType.Blood], [2, 2])); // Annakarl  teleport
            _ancientTeleports.Add(47,
                new StandardTeleportScript(MagicBook.AncientBook, 2972, 3873, 0, 2, 96, 106, [RuneType.Law, RuneType.Water], [2, 8])); // Ghorrock  teleport

            _lunarTeleports.Add(43,
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
            _lunarTeleports.Add(54,
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
            _lunarTeleports.Add(67,
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
            _lunarTeleports.Add(47,
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
            _lunarTeleports.Add(22,
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
            _lunarTeleports.Add(69,
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
            _lunarTeleports.Add(41,
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
            _lunarTeleports.Add(40,
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
            _lunarTeleports.Add(44,
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
            _lunarTeleports.Add(51,
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
        }

        /// <summary>
        ///     Loads the scripted combat spells.
        /// </summary>
        private static async Task LoadScriptedCombatSpells()
        {
            using var scope = ServiceLocator.Current.CreateScope();
            var magicManager = scope.ServiceProvider.GetRequiredService<IMagicService>();
            foreach (var definition in await magicManager.FindAllCombatSpells())
            {
                if (definition.ButtonId == 36 || definition.ButtonId == 55 || definition.ButtonId == 81)
                {
                    _normalCombatSpells.Add(definition.ButtonId, new HoldSpell(definition));
                }
                else if (definition.ButtonId == 66)
                {
                    _normalCombatSpells.Add(66, new SaradominStrike(definition));
                }
                else if (definition.ButtonId == 67)
                {
                    _normalCombatSpells.Add(67, new ClawsOfGuthix(definition));
                }
                else if (definition.ButtonId == 68)
                {
                    _normalCombatSpells.Add(68, new FlamesOfZamorak(definition));
                }
                else if (definition.ButtonId == 86)
                {
                    _normalCombatSpells.Add(86, new TeleBlock(definition));
                }
                else if (definition.ButtonId == 91)
                {
                    _normalCombatSpells.Add(91, new FireSurge(definition));
                }
                else if (definition.ButtonId == 99)
                {
                    _normalCombatSpells.Add(99, new StormOfArmadyl(definition));
                }
            }
        }

        private IGameConnectHandle _settingsChanged = default!;
        private IGameConnectHandle _bookChanged = default!;

        public SpellBookTab(
            ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator, IServiceProvider serviceProvider,
            IMagicService magicService, IItemBuilder itemBuilder) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
            _serviceProvider = serviceProvider;
            _magicService = magicService;
            _itemBuilder = itemBuilder;
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

            var magicManager = _serviceProvider.GetRequiredService<IMagicService>();

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
                                if (itemUsedOnSlot < 0 || itemUsedOnSlot >= Owner.Inventory.Capacity)
                                {
                                    return false;
                                }

                                var item = Owner.Inventory[itemUsedOnSlot];
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
                                if (itemUsedOnSlot < 0 || itemUsedOnSlot >= Owner.Inventory.Capacity)
                                {
                                    return false;
                                }

                                var item = Owner.Inventory[itemUsedOnSlot];
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

                                if (Owner.HasState(StateType.CantCastCharge))
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
                                        Owner.AddState(new State(StateType.CantCastCharge, 100)); // 1 minute
                                        Owner.AddState(new State(StateType.Charge, 700)); // 7 minutes
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

                        foreach (var definition in magicManager.FindAllCombatSpells().ConfigureAwait(false).GetAwaiter().GetResult())
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
                                    if (itemUsedOnSlot < 0 || itemUsedOnSlot >= Owner.Inventory.Capacity)
                                    {
                                        return false;
                                    }

                                    var item = Owner.Inventory[itemUsedOnSlot];
                                    if (item == null || item.Id != itemUsedOnId)
                                    {
                                        return false;
                                    }

                                    Owner.QueueTask(() => CastEnchant(Owner, item, definition));
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
                                if (Owner.HasState(StateType.CantCastVengeance))
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
                                        Owner.AddState(new State(StateType.CantCastVengeance, 50));
                                        Owner.AddState(new State(StateType.Vengeance, 500));
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

                        InterfaceInstance.AttachClickHandler(18,
                            (componentID, clickType, extra1, extra2) =>
                            {
                                if (clickType != ComponentClickType.LeftClick)
                                {
                                    return false;
                                }


                                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.CombatSettingsMagicDefensiveCasting));
                                return true;
                            });

                        foreach (short childID in _ancientTeleports.Keys)
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

                        OnComponentClick autoCastClickHandler = (componentID, clickType, extra1, extra2) =>
                        {
                            if (clickType != ComponentClickType.Option6Click)
                            {
                                return false;
                            }

                            switch (componentID)
                            {
                                case 20: AutoCast(_iceSpells[0]); break;
                                case 21: AutoCast(_iceSpells[2]); break;
                                case 22: AutoCast(_iceSpells[1]); break;
                                case 23: AutoCast(_iceSpells[3]); break;
                                case 24: AutoCast(_bloodSpells[0]); break;
                                case 25: AutoCast(_bloodSpells[2]); break;
                                case 26: AutoCast(_bloodSpells[1]); break;
                                case 27: AutoCast(_bloodSpells[3]); break;
                                case 28: AutoCast(_smokeSpells[0]); break;
                                case 29: AutoCast(_smokeSpells[2]); break;
                                case 30: AutoCast(_smokeSpells[1]); break;
                                case 31: AutoCast(_smokeSpells[3]); break;
                                case 32: AutoCast(_shadowSpells[0]); break;
                                case 33: AutoCast(_shadowSpells[2]); break;
                                case 34: AutoCast(_shadowSpells[1]); break;
                                case 35: AutoCast(_shadowSpells[3]); break;
                                case 36: AutoCast(_miasmicSpells[0]); break;
                                case 37: AutoCast(_miasmicSpells[2]); break;
                                case 38: AutoCast(_miasmicSpells[1]); break;
                                case 39: AutoCast(_miasmicSpells[3]); break;
                            }

                            return true;
                        };


                        OnComponentUsedOnCreature castUseHandler = (componentID, usedOn, shouldRun, extra1, extra2) =>
                        {
                            Owner.Interrupt(this);
                            if (componentID == 20)
                            {
                                Cast(_iceSpells[0], usedOn, shouldRun);
                            }

                            if (componentID == 21)
                            {
                                Cast(_iceSpells[2], usedOn, shouldRun);
                            }

                            if (componentID == 22)
                            {
                                Cast(_iceSpells[1], usedOn, shouldRun);
                            }

                            if (componentID == 23)
                            {
                                Cast(_iceSpells[3], usedOn, shouldRun);
                            }

                            if (componentID == 24)
                            {
                                Cast(_bloodSpells[0], usedOn, shouldRun);
                            }

                            if (componentID == 25)
                            {
                                Cast(_bloodSpells[2], usedOn, shouldRun);
                            }

                            if (componentID == 26)
                            {
                                Cast(_bloodSpells[1], usedOn, shouldRun);
                            }

                            if (componentID == 27)
                            {
                                Cast(_bloodSpells[3], usedOn, shouldRun);
                            }

                            if (componentID == 28)
                            {
                                Cast(_smokeSpells[0], usedOn, shouldRun);
                            }

                            if (componentID == 29)
                            {
                                Cast(_smokeSpells[2], usedOn, shouldRun);
                            }

                            if (componentID == 30)
                            {
                                Cast(_smokeSpells[1], usedOn, shouldRun);
                            }

                            if (componentID == 21)
                            {
                                Cast(_smokeSpells[3], usedOn, shouldRun);
                            }

                            if (componentID == 32)
                            {
                                Cast(_shadowSpells[0], usedOn, shouldRun);
                            }

                            if (componentID == 33)
                            {
                                Cast(_shadowSpells[2], usedOn, shouldRun);
                            }

                            if (componentID == 34)
                            {
                                Cast(_shadowSpells[1], usedOn, shouldRun);
                            }

                            if (componentID == 35)
                            {
                                Cast(_shadowSpells[3], usedOn, shouldRun);
                            }

                            if (componentID == 36)
                            {
                                Cast(_miasmicSpells[0], usedOn, shouldRun);
                            }

                            if (componentID == 37)
                            {
                                Cast(_miasmicSpells[2], usedOn, shouldRun);
                            }

                            if (componentID == 38)
                            {
                                Cast(_miasmicSpells[1], usedOn, shouldRun);
                            }

                            if (componentID == 39)
                            {
                                Cast(_miasmicSpells[3], usedOn, shouldRun);
                            }

                            return true;
                        };

                        for (var i = 20; i < 40; i++)
                        {
                            InterfaceInstance.AttachClickHandler(i, autoCastClickHandler);
                            InterfaceInstance.AttachUseOnCreatureHandler(i, castUseHandler);
                        }

                        break;
                    }
            }

            Refresh();
        }

        public async Task<bool> CastEnchant(ICharacter character, IItem item, EnchantingSpellDto dto)
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
            if (product == null)
            {
                character.SendChatMessage("You can not enchant this item!");
                return false;
            }

            character.Magic.RemoveRunes(dto.RequiredRunes, dto.RequiredRunesCounts);
            var slot = character.Inventory.GetInstanceSlot(item);
            if (slot == -1)
            {
                return false;
            }

            character.Inventory.Replace(slot, _itemBuilder.Create().WithId(product.ProductId).Build());
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
            Owner.Combat.SetTarget(target);
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