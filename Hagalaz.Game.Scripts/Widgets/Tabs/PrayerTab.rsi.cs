using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents prayer tab.
    /// </summary>
    public class PrayerTab : WidgetScript
    {
        private IGameConnectHandle _bookChanged = default!;
        private readonly IScopedGameMediator _gameMediator;

        public PrayerTab(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _bookChanged = _gameMediator.ConnectHandler<ProfileValueChanged<PrayerBook>>(async (context) =>
            {
                if (context.Message.Key == ProfileConstants.PrayerSettingsBook)
                {
                    Owner.Prayers.DeactivateAllPrayers();
                    RefreshBook();
                }
            });

            Owner.Configurations.SendGlobalCs2Int(181, 0); // 0 = prayer / 1 = quick prayer
            InterfaceInstance.SetOptions(8, 0, 30, 0x2); // allow first right click option
            InterfaceInstance.SetOptions(42, 0, 30, 2); // allow quick prayer selecting

            InterfaceInstance.AttachClickHandler(8, (componentID, clickType, extraData1, prayerID) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }
                var book = Owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
                if (book == PrayerBook.StandardBook)
                {
                    if (prayerID >= 0 && prayerID < PrayerConstants.StandardPrayers.Length)
                    {
                        if (Owner.Prayers.IsPraying(PrayerConstants.StandardPrayers[prayerID]))
                        {
                            Owner.Prayers.DeactivatePrayer(PrayerConstants.StandardPrayers[prayerID]);
                        }
                        else
                        {
                            Owner.Prayers.ActivatePrayer(PrayerConstants.StandardPrayers[prayerID]);
                        }
                    }
                }
                else if (book == PrayerBook.CursesBook)
                {
                    if (prayerID >= 0 && prayerID < PrayerConstants.Curses.Length)
                    {
                        if (Owner.Prayers.IsPraying(PrayerConstants.Curses[prayerID]))
                        {
                            Owner.Prayers.DeactivatePrayer(PrayerConstants.Curses[prayerID]);
                        }
                        else
                        {
                            Owner.Prayers.ActivatePrayer(PrayerConstants.Curses[prayerID]);
                        }
                    }
                }

                return true;
            });

            InterfaceInstance.AttachClickHandler(42, (componentID, clickType, extraData1, prayerID) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var book = Owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
                    if (book == PrayerBook.StandardBook)
                    {
                        if (prayerID >= 0 && prayerID < PrayerConstants.StandardPrayers.Length)
                        {
                            if (Owner.Prayers.QuickPrayer.IsQuickPraying(PrayerConstants.StandardPrayers[prayerID]))
                            {
                                Owner.Prayers.QuickPrayer.DeactivateQuickPrayer(PrayerConstants.StandardPrayers[prayerID]);
                            }
                            else
                            {
                                Owner.Prayers.QuickPrayer.ActivateQuickPrayer(PrayerConstants.StandardPrayers[prayerID]);
                            }
                        }
                    }
                    else if (book == PrayerBook.CursesBook)
                    {
                        if (prayerID >= 0 && prayerID < PrayerConstants.Curses.Length)
                        {
                            if (Owner.Prayers.QuickPrayer.IsQuickPraying(PrayerConstants.Curses[prayerID]))
                            {
                                Owner.Prayers.QuickPrayer.DeactivateQuickPrayer(PrayerConstants.Curses[prayerID]);
                            }
                            else
                            {
                                Owner.Prayers.QuickPrayer.ActivateQuickPrayer(PrayerConstants.Curses[prayerID]);
                            }
                        }
                    }

                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(43, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Prayers.QuickPrayer.SelectingQuickPrayers = false;
                    return true;
                }

                return false;
            });

            Refresh();
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            _bookChanged?.Disconnect();
        }

        private void Refresh()
        {
            RefreshBook();
            Owner.Prayers.RefreshConfigurations();
        }

        private void RefreshBook()
        {
            var book = Owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
            Owner.Configurations.SendStandardConfiguration(1584, (int)book);
        }
    }
}