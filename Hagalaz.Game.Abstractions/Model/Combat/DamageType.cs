namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines combat damage types.
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// Standard melee attack damage. Normal NPC's use this.
        /// Protection prayers protect fully against this type of damage
        /// </summary>
        StandardMelee,
        /// <summary>
        /// Standard range attack damage. Normal NPC's use this.
        /// Protection prayers protect fully against this type of damage
        /// </summary>
        StandardRange,
        /// <summary>
        /// Standard magic attack damage. Normal NPC's use this.
        /// Protection prayers protect fully against this type of damage
        /// </summary>
        StandardMagic,
        /// <summary>
        /// Standard summoning attack damage. Normal NPC's use this.
        /// Protection prayers protect fully against this type of damage
        /// </summary>
        StandardSummoning,
        /// <summary>
        /// 
        /// </summary>
        Reflected,
        /// <summary>
        /// Standard attack damage.
        /// Protection prayers are not included in calculations.
        /// </summary>
        Standard,
        /// <summary>
        /// 
        /// </summary>
        DragonFire,
        /// <summary>
        /// Full melee attack damage. Some NPC's and all characters use this.  
        /// Protection prayers protects you from most of the damage of this type.
        /// </summary>
        FullMelee,
        /// <summary>
        /// Full range attack damage. Some NPC's and all characters use this. 
        /// Protection prayers protects you from most of the damage of this type.
        /// </summary>
        FullRange,
        /// <summary>
        /// Full magic attack damage. Some NPC's and all characters use this. 
        /// Protection prayers protects you from most of the damage of this type.
        /// </summary>
        FullMagic,
        /// <summary>
        /// Full summoning attack damage. Some NPC's and all characters use this. 
        /// Protection prayers protects you from most of the damage of this type.
        /// </summary>
        FullSummoning,
    }
}
