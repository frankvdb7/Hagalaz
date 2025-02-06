namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IQuickPrayer
    {
        /// <summary>
        /// Gets the quick prayer status.
        /// </summary>
        /// <value>
        /// The quick prayer status.
        /// </value>
        int[] QuickPrayerStatus { get; }
        /// <summary>
        /// Gets a value indicating whether [selected quick prayers].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [selected quick prayers]; otherwise, <c>false</c>.
        /// </value>
        bool SelectedQuickPrayers { get; }
        /// <summary>
        /// Gets or sets a value indicating whether [selecting quick prayers].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [selecting quick prayers]; otherwise, <c>false</c>.
        /// </value>
        bool SelectingQuickPrayers { get; set; }
        /// <summary>
        /// Sets the quick prayer status.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        void SetQuickPrayerStatus(int index, int value);
        /// <summary>
        /// Turns the off quick prayer.
        /// </summary>
        void TurnOffQuickPrayer();
        /// <summary>
        /// Activates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        void ActivateQuickPrayer(NormalPrayer prayer);
        /// <summary>
        /// Switches the quick prayer.
        /// </summary>
        void SwitchQuickPrayer();
        /// <summary>
        /// Activates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        void ActivateQuickPrayer(AncientCurses prayer);
        /// <summary>
        /// Deactivates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        void DeactivateQuickPrayer(NormalPrayer prayer);
        /// <summary>
        /// Deactivates the quick prayer.
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        void DeactivateQuickPrayer(AncientCurses prayer);
        /// <summary>
        /// Determines whether [is quick praying] [the specified prayer].
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        /// <returns>
        ///   <c>true</c> if [is quick praying] [the specified prayer]; otherwise, <c>false</c>.
        /// </returns>
        bool IsQuickPraying(NormalPrayer prayer);
        /// <summary>
        /// Determines whether [is quick praying] [the specified prayer].
        /// </summary>
        /// <param name="prayer">The prayer.</param>
        /// <returns>
        ///   <c>true</c> if [is quick praying] [the specified prayer]; otherwise, <c>false</c>.
        /// </returns>
        bool IsQuickPraying(AncientCurses prayer);
    }
}
