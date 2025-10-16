namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a character's prayers and curses, including activation, deactivation, and point drain.
    /// </summary>
    public interface IPrayers
    {
        /// <summary>
        /// Gets a value indicating whether the character has at least one prayer or curse active.
        /// </summary>
        bool Praying { get; }
        /// <summary>
        /// Gets the handler for the character's quick prayers setup.
        /// </summary>
        IQuickPrayer QuickPrayer { get; }
        /// <summary>
        /// Processes a single game tick for prayers, handling prayer point drain.
        /// </summary>
        void Tick();
        /// <summary>
        /// Checks if a specific standard prayer is currently active.
        /// </summary>
        /// <param name="prayer">The prayer to check.</param>
        /// <returns><c>true</c> if the prayer is active; otherwise, <c>false</c>.</returns>
        bool IsPraying(NormalPrayer prayer);
        /// <summary>
        /// Checks if a specific ancient curse is currently active.
        /// </summary>
        /// <param name="prayer">The curse to check.</param>
        /// <returns><c>true</c> if the curse is active; otherwise, <c>false</c>.</returns>
        bool IsPraying(AncientCurses prayer);
        /// <summary>
        /// Activates a standard prayer, deactivating any conflicting prayers.
        /// </summary>
        /// <param name="prayer">The prayer to activate.</param>
        void ActivatePrayer(NormalPrayer prayer);
        /// <summary>
        /// Activates an ancient curse, deactivating any conflicting curses.
        /// </summary>
        /// <param name="prayer">The curse to activate.</param>
        void ActivatePrayer(AncientCurses prayer);
        /// <summary>
        /// Deactivates a standard prayer.
        /// </summary>
        /// <param name="prayer">The prayer to deactivate.</param>
        void DeactivatePrayer(NormalPrayer prayer);
        /// <summary>
        /// Deactivates an ancient curse.
        /// </summary>
        /// <param name="prayer">The curse to deactivate.</param>
        void DeactivatePrayer(AncientCurses prayer);
        /// <summary>
        /// Deactivates all currently active prayers and curses.
        /// </summary>
        void DeactivateAllPrayers();
        /// <summary>
        /// Refreshes any dynamic prayer bonuses that change over time (e.g., Turmoil).
        /// </summary>
        void RefreshDynamicBonuses();
        /// <summary>
        /// Sends the current prayer configuration (e.g., active prayers, prayer points) to the client.
        /// </summary>
        void RefreshConfigurations();
    }
}
