namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPrayers
    {
        /// <summary>
        /// Tell's if character is currently praying.
        /// </summary>
        bool Praying { get; }
        /// <summary>
        /// Gets the quick prayer.
        /// </summary>
        IQuickPrayer QuickPrayer { get; }
        /// <summary>
        /// Tick's character's prayers.
        /// </summary>
        void Tick();
        /// <summary>
        /// Get's if character is praying specific prayer.
        /// </summary>
        /// <returns></returns>
        bool IsPraying(NormalPrayer prayer);
        /// <summary>
        /// Get's if character is praying specific prayer.
        /// </summary>
        /// <returns></returns>
        bool IsPraying(AncientCurses prayer);
        /// <summary>
        /// Activate's specific prayer.
        /// This method does nothing if prayer is already activated or book is not
        /// standart book.
        /// </summary>
        void ActivatePrayer(NormalPrayer prayer);
        /// <summary>
        /// Activate's specific curse;
        /// This method does nothing if prayer is already activated or book is not
        /// standart book.
        /// </summary>
        void ActivatePrayer(AncientCurses prayer);
        /// <summary>
        /// Deactivate's specific prayer.
        /// This method does nothing if prayer is not activated.
        /// </summary>
        /// <param name="prayer"></param>
        void DeactivatePrayer(NormalPrayer prayer);
        /// <summary>
        /// Deactivate's specific prayer.
        /// This method does nothing if prayer is not activated.
        /// </summary>
        /// <param name="prayer"></param>
        void DeactivatePrayer(AncientCurses prayer);
        /// <summary>
        /// Deactivate's all prayers and curses.
        /// </summary>
        void DeactivateAllPrayers();
        /// <summary>
        /// Refreshe's dynamic bonuses.
        /// </summary>
        void RefreshDynamicBonuses();
        /// <summary>
        /// Refreshes the configurations.
        /// </summary>
        void RefreshConfigurations();
    }
}
