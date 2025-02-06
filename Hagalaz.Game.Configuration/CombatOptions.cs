namespace Hagalaz.Game.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public class CombatOptions
    {
        public const string Key = "Combat";
        
        /// <summary>
        /// 
        /// </summary>
        public double ExpRate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int CharacterAttackTickDelay { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int NpcAttackTickDelay { get; set; }
    }
}