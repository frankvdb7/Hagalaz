using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    public interface INpcDefinition : INpcType
    {
        /// <summary>
        /// The NPC's display name.
        /// </summary>
        string DisplayName { get; set; }
        /// <summary>
        /// Contains boolean if this npc is attackable.
        /// </summary>
        bool Attackable { get; set; }
        /// <summary>
        /// Contains max hitpoints of this npc.
        /// </summary>
        int MaxLifePoints { get; set; }
        /// <summary>
        /// Contains boolean if this npc walks randomly.
        /// </summary>
        bool WalksRandomly { get; set; }
        /// <summary>
        /// Contains npc reaction.
        /// </summary>
        ReactionType ReactionType { get; set; }
        /// <summary>
        /// Contains npc walk type.
        /// </summary>
        BoundsType BoundsType { get; set; }
        /// <summary>
        /// Contains the pick pocketing loot table Id.
        /// </summary>
        int PickPocketingLootTableId { get; set; }
        /// <summary>
        /// Contains npc attack level.
        /// </summary>
        int MaxAttackLevel { get; set; }
        /// <summary>
        /// Contains npc strength level.
        /// </summary>
        int MaxStrengthLevel { get; set; }
        /// <summary>
        /// Contains npc defence level.
        /// </summary>
        int MaxDefenceLevel { get; set; }
        /// <summary>
        /// Contains npc ranged level.
        /// </summary>
        int MaxRangedLevel { get; set; }
        /// <summary>
        /// Contains npc magic level.
        /// </summary>
        int MaxMagicLevel { get; set; }
        /// <summary>
        /// The NPC's examine text.
        /// </summary>
        string Examine { get; set; }
        /// <summary>
        /// Contains npc attack speed.
        /// </summary>
        int AttackSpeed { get; set; }
        /// <summary>
        /// Contains npc attack animation.
        /// </summary>
        int AttackAnimation { get; set; }
        /// <summary>
        /// Contains npc attack graphic.
        /// </summary>
        int AttackGraphic { get; set; }
        /// <summary>
        /// Contains npc defence animation.
        /// </summary>
        int DefenceAnimation { get; set; }
        /// <summary>
        /// Contains npc defence graphic.
        /// </summary>
        int DefenseGraphic { get; set; }
        /// <summary>
        /// Contains boolean if this npc is poisonable.
        /// </summary>
        bool Poisonable { get; set; }
        /// <summary>
        /// Contains npc death animation.
        /// </summary>
        int DeathAnimation { get; set; }
        /// <summary>
        /// Contains npc death graphic.
        /// </summary>
        int DeathGraphic { get; set; }
        /// <summary>
        /// Contains the death ticks.
        /// </summary>
        int DeathTicks { get; set; }
        /// <summary>
        /// Contains this definition bonuses.
        /// </summary>
        IBonuses Bonuses { get; set; }
        /// <summary>
        /// Gets or sets the slayer level required.
        /// </summary>
        /// <value>
        /// The slayer level required.
        /// </value>
        int SlayerLevelRequired { get; set; }
        /// <summary>
        /// Contains npc respawn time.
        /// </summary>
        int RespawnTime { get; set; }
        /// <summary>
        /// Contains the loot table id.
        /// </summary>
        int LootTableId { get; set; }
    }
}
