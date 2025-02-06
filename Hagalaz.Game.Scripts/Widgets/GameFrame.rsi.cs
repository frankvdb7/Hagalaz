using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Orbs;
using Hagalaz.Game.Scripts.Widgets.PriceCheck;
using Hagalaz.Game.Scripts.Widgets.Skills;
using Hagalaz.Game.Scripts.Widgets.Tabs;

namespace Hagalaz.Game.Scripts.Widgets
{
    /// <summary>
    ///     Represents game frame script.
    /// </summary>
    [WidgetScriptMetaData([548, 746])]
    public class GameFrame : WidgetScript
    {
        private readonly IScopedGameMediator _gameMediator;
        private readonly IWidgetBuilder _widgetBuilder;

        private IGameConnectHandle _toggleChanged = default!;

        /// <summary>
        ///     Contains money change handler.
        /// </summary>
        private EventHappened? _moneyChangeHandler;

        /// <summary>
        ///     Contains money X handler.
        /// </summary>
        private OnIntInput? _moneyXHandler;

        public GameFrame(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator, IWidgetBuilder widgetBuilder) : base(
            characterContextAccessor)
        {
            _gameMediator = gameMediator;
            _widgetBuilder = widgetBuilder;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _toggleChanged = _gameMediator.ConnectHandler<ProfileValueChanged<bool>>((context) =>
            {
                if (context.Message.Key == ProfileConstants.MoneyPouchSettingsToggled)
                {
                    RefreshMoneyPouchToggle();
                }

                if (context.Message.Key == ProfileConstants.XpCounterSettingsToggled)
                {
                    RefreshXpCounterToggle();
                }

                if (context.Message.Key == ProfileConstants.XpCounterSettingsPopupToggled)
                {
                    RefreshXpCounterPopUp();
                }
            });

            // chat options box
            var chatOptionsBox = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(751)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 56 : 64)
                .WithTransparency(1)
                .WithScript<ChatOptionsBox>()
                .Build();
            Owner.Widgets.OpenWidget(chatOptionsBox);
            // chat box frame
            var chatFrame = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId((int)InterfaceIds.ChatboxFrame)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 171 : 63)
                .Build();
            Owner.Widgets.OpenWidget(chatFrame);
            // xp counter
            var xpCounter = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(754)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 45 : 67)
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(xpCounter);
            // xp counter real
            var xpCounterReal = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(1215)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 53 : 244)
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(xpCounterReal);
            // hp orb
            var hpOrb = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(748)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 163 : 238)
                .WithScript<ConstitutionOrb>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(hpOrb);
            // prayer orb
            var prayerOrb = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(749)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 164 : 239)
                .WithScript<PrayerOrb>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(prayerOrb);
            // run orb
            var runOrb = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(750)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 165 : 240)
                .WithScript<RunEnergyOrb>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(runOrb);
            // summoning orb
            var summoningOrb = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(747)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 167 : 241)
                .WithScript<SummoningOrb>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(summoningOrb);
            // ??
            var unknown = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(745)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 43 : 57)
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(unknown);
            // chat talking box
            var chatTalkingBox = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(137)
                .WithParentId(chatFrame.Id)
                .WithParentSlot(9)
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(chatTalkingBox);
            // attack tab
            var attackTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(884)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 179 : 154)
                .WithScript<CombatTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(attackTab);
            // notice board tab
            var noticeBoardTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(1056)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 180 : 155)
                .WithScript<NoticeBoardTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(noticeBoardTab);
            // skill tab
            var skillTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(320)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 181 : 156)
                .WithScript<SkillsTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(skillTab);
            // quest tab - interface id: 190
            var questTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(930)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 182 : 157)
                .WithScript<QuestTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(questTab);
            // inventory tab
            var inventoryTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId((int)InterfaceIds.Inventory)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? (int)InterfaceSlots.FixedInventorySlot : (int)InterfaceSlots.ResizedInventorySlot)
                .WithScript<InventoryTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(inventoryTab);
            // equipment tab
            var equipmentTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(387)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 184 : 159)
                .WithScript<Tabs.EquipmentTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(equipmentTab);
            // prayer tab
            var prayerTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(271)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 185 : 160)
                .WithScript<PrayerTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(prayerTab);
            // magic tab
            var book = Owner.Profile.GetValue(ProfileConstants.MagicSettingsBook, MagicBook.StandardBook);
            var magicTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId((int)book)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 186 : 161)
                .WithScript<SpellBookTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(magicTab);
            // Friends n Ignores tab
            var friendsTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(550)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 188 : 163)
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(friendsTab);
            // Friends chat tab
            var friendsChatTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(1109)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 189 : 164)
                .WithScript<FriendsChatTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(friendsChatTab);
            // Clan tab
            var clanTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(1110)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 190 : 165)
                .WithScript<ClanChatTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(clanTab);
            // options tab
            var optionsTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(261)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 191 : 166)
                .WithScript<OptionsTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(optionsTab);
            // emote tab
            var emotesTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(590)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 192 : 167)
                .WithScript<EmotesTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(emotesTab);
            // music tab
            var musicTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(187)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 193 : 168)
                .WithScript<MusicTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(musicTab);
            // note tab
            var notesTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(34)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 194 : 169)
                .WithScript<NotesTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(notesTab);
            // Logout tab
            var logoutTab = _widgetBuilder.Create()
                .ForCharacter(Owner)
                .WithId(182)
                .WithParentSlot(Owner.GameClient.IsScreenFixed ? 197 : 172)
                .WithScript<LogoutTab>()
                .WithTransparency(1)
                .Build();
            Owner.Widgets.OpenWidget(logoutTab);

            // main configs
            Owner.Configurations.SendStandardConfiguration(281, 1000); // disable tutorial mode
            Owner.Configurations.SendStandardConfiguration(2527,
                Owner.Permissions.HasAtLeastXPermission(Permission.Donator) ? 1 : 0); // enable option to disable navigation bar

            //this.owner.Configurations.SendStandartConfiguration(1160, -1); // unlock summoning orb

            Owner.Configurations.SendStandardConfiguration(1232, 250); // 'complete' smoking kills quest, to enable slayer interface buttons

            Owner.Statistics.RefreshXpCounters();

            RefreshMoneyPouch(Owner.MoneyPouch.Count);

            RefreshMoneyPouchToggle();

            RefreshXpCounterToggle();

            RefreshXpCounterPopUp();

            Setup();
        }

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            _moneyChangeHandler = Owner.RegisterEventHandler(new EventHappened<MoneyPouchChangedEvent>(e =>
            {
                RefreshMoneyPouch(e.PreviousCount, e.Count);
                RefreshMoneyPouch(e.Count);
                return false;
            }));

            // xp orb
            InterfaceInstance.AttachClickHandler(Owner.GameClient.IsScreenFixed ? 38 : 97,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.XpCounterSettingsToggled));
                        return true;
                    }

                    if (type == ComponentClickType.Option2Click)
                    {
                        _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.XpCounterSettingsPopupToggled));
                        Owner.SendChatMessage(Owner.Profile.GetValue<bool>(ProfileConstants.XpCounterSettingsPopupToggled)
                            ? "The XP counter pop-ups are now enabled."
                            : "The XP counter pop-ups are now disabled.");
                        return true;
                    }

                    if (type == ComponentClickType.Option3Click)
                    {
                        var xpCounter = Owner.ServiceProvider.GetRequiredService<XpCounterSetup>();
                        Owner.Widgets.OpenWidget(1214, 0, xpCounter, true);
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(Owner.GameClient.IsScreenFixed ? 160 : 242,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        var frame = _widgetBuilder.Create()
                            .ForCharacter(Owner)
                            .WithId(755)
                            .WithScript<WorldMap>()
                            .AsFrame()
                            .Build();
                        Owner.Widgets.OpenFrame(frame);
                        return true;
                    }

                    return false;
                });

            // money pouch toggle
            InterfaceInstance.AttachClickHandler(Owner.GameClient.IsScreenFixed ? 201 : 247,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MoneyPouchSettingsToggled));
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(Owner.GameClient.IsScreenFixed ? 170 : 250,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MoneyPouchSettingsToggled));
                        return true;
                    }

                    if (type == ComponentClickType.Option2Click)
                    {
                        _moneyXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _moneyXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }
                            else
                            {
                                Owner.MoneyPouch.MoveToInventory(value);
                            }
                        };
                        Owner.Configurations.SendIntegerInput(Owner.MoneyPouch.Examine + "<br>How many would you like to withdraw?");
                        return true;
                    }

                    if (type == ComponentClickType.Option3Click)
                    {
                        Owner.SendChatMessage(Owner.MoneyPouch.Examine);
                        return true;
                    }

                    if (type == ComponentClickType.Option4Click)
                    {
                        if (Owner.IsBusy())
                        {
                            Owner.SendChatMessage("Please finish what you're doing before opening price checker interface.");
                            return true;
                        }

                        Owner.Widgets.OpenWidget(206, 0, Owner.ServiceProvider.GetRequiredService<PriceChecker>(), true);
                        return true;
                    }

                    return false;
                });
        }

        /// <summary>
        ///     Refreshes the money pouch.
        /// </summary>
        /// <param name="previousCount">The previous amount.</param>
        /// <param name="count">The amount.</param>
        public void RefreshMoneyPouch(int previousCount, int count)
        {
            int diff;
            if (previousCount > count)
            {
                diff = previousCount - count;
            }
            else
            {
                diff = count - previousCount;
            }

            Owner.Configurations.SendCs2Script(5561, [diff, previousCount > count ? 0 : 1]);
        }

        /// <summary>
        ///     Refreshes the money pouch.
        /// </summary>
        /// <param name="count">The amount.</param>
        public void RefreshMoneyPouch(int count) => Owner.Configurations.SendCs2Script(5560, [count]);

        public void RefreshMoneyPouchToggle() =>
            Owner.Configurations.SendCs2Script(5557,
            [
                Owner.Profile.GetValue<bool>(ProfileConstants.MoneyPouchSettingsToggled) ? 1 : 0
            ]);

        public void RefreshXpCounterToggle()
        {
            // TODO
        }

        public void RefreshXpCounterPopUp()
        {
            if (Owner.Profile.GetValue<bool>(ProfileConstants.XpCounterSettingsPopupToggled))
            {
                Owner.Widgets.OpenWidget(1213,
                    Owner.GameClient.IsScreenFixed ? 51 : 83,
                    1,
                    Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>(),
                    false);
            }
            else
            {
                var popup = Owner.Widgets.GetOpenWidget(1213);
                if (popup != null) Owner.Widgets.CloseWidget(popup);
            }
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            _toggleChanged?.Disconnect();

            if (_moneyChangeHandler != null)
            {
                Owner.UnregisterEventHandler<MoneyPouchChangedEvent>(_moneyChangeHandler);
            }

            if (_moneyXHandler == Owner.Widgets.IntInputHandler)
            {
                if (_moneyXHandler != null)
                {
                    if (Owner.Widgets.IntInputHandler == _moneyXHandler)
                    {
                        Owner.Widgets.IntInputHandler = null;
                    }

                    _moneyXHandler = null;
                }
            }
        }
    }
}