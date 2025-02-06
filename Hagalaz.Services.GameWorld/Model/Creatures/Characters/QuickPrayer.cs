using System.Linq;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Contains quick prayer.
    /// </summary>
    public class QuickPrayer : IQuickPrayer
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly int[] _quickPrayerStatus;

        /// <summary>
        /// 
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// 
        /// </summary>
        private bool _selecting = false;

        /// <summary>
        /// Gets a value indicating whether [quick praying].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [quick praying]; otherwise, <c>false</c>.
        /// </value>
        public bool QuickPraying { get; private set; }

        /// <summary>
        /// Gets the quick prayer status.
        /// </summary>
        /// <value>
        /// The quick prayer status.
        /// </value>
        public int[] QuickPrayerStatus => _quickPrayerStatus;

        /// <summary>
        /// Gets or sets a value indicating whether [selecting quick prayers].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [selecting quick prayers]; otherwise, <c>false</c>.
        /// </value>
        public bool SelectingQuickPrayers
        {
            get => _selecting;
            set
            {
                if (value)
                {
                    DeactivateAllQuickPrayers();
                    _owner.Configurations.SendStandardConfiguration(1396, 1);
                    _owner.Configurations.SendGlobalCs2Int(181, 1);
                    _owner.Configurations.SendGlobalCs2Int(168, 6);
                }
                else
                {
                    _owner.Configurations.SendStandardConfiguration(1396, 0);
                    _owner.Configurations.SendGlobalCs2Int(181, 0);
                    _owner.Configurations.SendGlobalCs2Int(168, 6);
                }

                _selecting = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [selected quick prayers].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [selected quick prayers]; otherwise, <c>false</c>.
        /// </value>
        public bool SelectedQuickPrayers
        {
            get
            {
                for (var i = 0; i < _quickPrayerStatus.Length; i++)
                {
                    if (_quickPrayerStatus[i] != 0)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPrayer" /> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public QuickPrayer(ICharacter owner)
        {
            _owner = owner;
            _quickPrayerStatus = new int[30];
        }

        /// <summary>
        /// Turns the off quick prayer.
        /// </summary>
        public void TurnOffQuickPrayer()
        {
            _owner.Prayers.DeactivateAllPrayers();
            QuickPraying = false;
            _owner.Configurations.SendGlobalCs2Int(182, 0);
        }

        /// <summary>
        /// Switches the quick prayer.
        /// </summary>
        public void SwitchQuickPrayer()
        {
            if (!SelectedQuickPrayers)
            {
                _owner.SendChatMessage("You have not selected any quick prayers!");
                return;
            }

            _owner.Prayers.DeactivateAllPrayers();
            if (!QuickPraying)
            {
                var book = _owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
                switch (book)
                {
                    case PrayerBook.StandardBook:
                        {
                            for (var i = 0; i < PrayerConstants.StandardPrayers.Length; i++)
                                if (_quickPrayerStatus[i] != 0)
                                    _owner.Prayers.ActivatePrayer(PrayerConstants.StandardPrayers[i]);
                            break;
                        }
                    case PrayerBook.CursesBook:
                        {
                            for (var i = 0; i < PrayerConstants.Curses.Length; i++)
                                if (_quickPrayerStatus[i] != 0)
                                    _owner.Prayers.ActivatePrayer(PrayerConstants.Curses[i]);
                            break;
                        }
                }

                QuickPraying = _owner.Prayers.Praying;
            }
            else
                QuickPraying = false;

            _owner.Configurations.SendGlobalCs2Int(182, QuickPraying ? 1 : 0);
        }

        /// <summary>
        /// Sets the quick prayer status.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public void SetQuickPrayerStatus(int index, int value) => _quickPrayerStatus[index] = value;

        /// <summary>
        /// Activates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        public void ActivateQuickPrayer(NormalPrayer prayer) => _quickPrayerStatus[(int)prayer & 0xFF] = (int)prayer;

        /// <summary>
        /// Activates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        public void ActivateQuickPrayer(AncientCurses prayer) => _quickPrayerStatus[(int)prayer & 0xFF] = (int)prayer;

        /// <summary>
        /// Deactivates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        public void DeactivateQuickPrayer(NormalPrayer prayer) => _quickPrayerStatus[(int)prayer & 0xFF] = 0;

        /// <summary>
        /// Deactivates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        public void DeactivateQuickPrayer(AncientCurses prayer) => _quickPrayerStatus[(int)prayer & 0xFF] = 0;

        /// <summary>
        /// Deactivates all quick prayers.
        /// </summary>
        private void DeactivateAllQuickPrayers()
        {
            foreach (var status in _quickPrayerStatus.Where(status => status != 0))
                if ((status & 0x4000) == 0)
                    DeactivateQuickPrayer((NormalPrayer)status);
                else
                    DeactivateQuickPrayer((AncientCurses)status);
        }

        /// <summary>
        /// Determines whether [is quick praying] [the specified prayer].
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        /// <returns>
        ///   <c>true</c> if [is quick praying] [the specified prayer]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsQuickPraying(NormalPrayer prayer) => _quickPrayerStatus[(int)prayer & 0xFF] == (int)prayer;

        /// <summary>
        /// Determines whether [is quick praying] [the specified prayer].
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        /// <returns>
        ///   <c>true</c> if [is quick praying] [the specified prayer]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsQuickPraying(AncientCurses prayer) => _quickPrayerStatus[(int)prayer & 0xFF] == (int)prayer;
    }
}