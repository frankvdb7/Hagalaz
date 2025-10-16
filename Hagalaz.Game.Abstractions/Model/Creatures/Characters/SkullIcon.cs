namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different skull icons that can be displayed above a character's head, typically to indicate a PvP status.
    /// </summary>
    public enum SkullIcon : byte
    {
        /// <summary>
        /// No icon is displayed.
        /// </summary>
        None = unchecked((byte)-1),
        /// <summary>
        /// The default white skull, indicating the player is an attacker in PvP.
        /// </summary>
        DefaultSkull = 0,
        /// <summary>
        /// A red skull, often used for high-risk PvP scenarios.
        /// </summary>
        DefaultSkullRed = 1,
        /// <summary>
        /// A bounty hunter skull indicating a tier 5 target.
        /// </summary>
        BountyLevelFive = 2,
        /// <summary>
        /// A bounty hunter skull indicating a tier 4 target.
        /// </summary>
        BountyLevelFour = 3,
        /// <summary>
        /// A bounty hunter skull indicating a tier 3 target.
        /// </summary>
        BountyLevelThree = 4,
        /// <summary>
        /// A bounty hunter skull indicating a tier 2 target.
        /// </summary>
        BountyLevelTwo = 5,
        /// <summary>
        /// A bounty hunter skull indicating a tier 1 target.
        /// </summary>
        BountyLevelOne = 6,
        /// <summary>
        /// A ghoul-themed skull, possibly for an event or minigame.
        /// </summary>
        Gool = 7,
        /// <summary>
        /// A skull indicating a player is attackable in the Crucible.
        /// </summary>
        CrucibleAttackable = 14,
        /// <summary>
        /// A skull indicating a player has immunity in the Crucible.
        /// </summary>
        CrucibleImmunity = 15,
        /// <summary>
        /// A skull indicating a player is skulled in the Crucible.
        /// </summary>
        CrucibleSkulled = 16
    }
}
