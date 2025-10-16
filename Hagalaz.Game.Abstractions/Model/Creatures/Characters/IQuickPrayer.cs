namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a character's quick prayers setup.
    /// </summary>
    public interface IQuickPrayer
    {
        /// <summary>
        /// Gets an array representing the status of each prayer in the quick prayer setup.
        /// </summary>
        int[] QuickPrayerStatus { get; }
        /// <summary>
        /// Gets a value indicating whether any quick prayers have been selected.
        /// </summary>
        bool SelectedQuickPrayers { get; }
        /// <summary>
        /// Gets or sets a value indicating whether the character is currently in the process of selecting their quick prayers.
        /// </summary>
        bool SelectingQuickPrayers { get; set; }
        /// <summary>
        /// Sets the status of a prayer at a specific index in the quick prayer setup.
        /// </summary>
        /// <param name="index">The index of the prayer to set.</param>
        /// <param name="value">The new status value.</param>
        void SetQuickPrayerStatus(int index, int value);
        /// <summary>
        /// Turns off all active quick prayers.
        /// </summary>
        void TurnOffQuickPrayer();
        /// <summary>
        /// Activates a specific standard prayer as part of the quick prayer setup.
        /// </summary>
        /// <param name="prayer">The prayer to activate.</param>
        void ActivateQuickPrayer(NormalPrayer prayer);
        /// <summary>
        /// Toggles the activation state of the currently selected quick prayers.
        /// </summary>
        void SwitchQuickPrayer();
        /// <summary>
        /// Activates a specific ancient curse as part of the quick prayer setup.
        /// </summary>
        /// <param name="prayer">The curse to activate.</param>
        void ActivateQuickPrayer(AncientCurses prayer);
        /// <summary>
        /// Deactivates a specific standard prayer from the quick prayer setup.
        /// </summary>
        /// <param name="prayer">The prayer to deactivate.</param>
        void DeactivateQuickPrayer(NormalPrayer prayer);
        /// <summary>
        /// Deactivates a specific ancient curse from the quick prayer setup.
        /// </summary>
        /// <param name="prayer">The curse to deactivate.</param>
        void DeactivateQuickPrayer(AncientCurses prayer);
        /// <summary>
        /// Checks if a specific standard prayer is part of the active quick prayer set.
        /// </summary>
        /// <param name="prayer">The prayer to check.</param>
        /// <returns><c>true</c> if the prayer is an active quick prayer; otherwise, <c>false</c>.</returns>
        bool IsQuickPraying(NormalPrayer prayer);
        /// <summary>
        /// Checks if a specific ancient curse is part of the active quick prayer set.
        /// </summary>
        /// <param name="prayer">The curse to check.</param>
        /// <returns><c>true</c> if the curse is an active quick prayer; otherwise, <c>false</c>.</returns>
        bool IsQuickPraying(AncientCurses prayer);
    }
}
