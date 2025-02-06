using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class PrayerAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the prayer.
        /// </summary>
        public NormalPrayer Prayer { get; }

        /// <summary>
        /// Contains the curse.
        /// </summary>
        public AncientCurses Curse { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrayerAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="prayer">The prayer.</param>
        public PrayerAllowEvent(ICharacter c, NormalPrayer prayer) : base(c) => Prayer = prayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrayerAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="curse">The curse.</param>
        public PrayerAllowEvent(ICharacter c, AncientCurses curse)
            : base(c) =>
            Curse = curse;
    }
}