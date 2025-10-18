using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for an NPC's data definition, containing its base stats, animations, and other non-volatile properties.
    /// </summary>
    public interface INpcDefinition : INpcType
    {
        /// <summary>
        /// Gets or sets the NPC's display name.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this NPC can be attacked.
        /// </summary>
        bool Attackable { get; set; }

        /// <summary>
        /// Gets or sets the maximum life points of this NPC.
        /// </summary>
        int MaxLifePoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this NPC walks randomly.
        /// </summary>
        bool WalksRandomly { get; set; }

        /// <summary>
        /// Gets or sets the NPC's reaction type (e.g., aggressive, passive).
        /// </summary>
        ReactionType ReactionType { get; set; }

        /// <summary>
        /// Gets or sets the NPC's movement boundary type.
        /// </summary>
        BoundsType BoundsType { get; set; }

        /// <summary>
        /// Gets or sets the ID of the loot table used when pickpocketing this NPC.
        /// </summary>
        int PickPocketingLootTableId { get; set; }

        /// <summary>
        /// Gets or sets the NPC's maximum Attack level.
        /// </summary>
        int MaxAttackLevel { get; set; }

        /// <summary>
        /// Gets or sets the NPC's maximum Strength level.
        /// </summary>
        int MaxStrengthLevel { get; set; }

        /// <summary>
        /// Gets or sets the NPC's maximum Defence level.
        /// </summary>
        int MaxDefenceLevel { get; set; }

        /// <summary>
        /// Gets or sets the NPC's maximum Ranged level.
        /// </summary>
        int MaxRangedLevel { get; set; }

        /// <summary>
        /// Gets or sets the NPC's maximum Magic level.
        /// </summary>
        int MaxMagicLevel { get; set; }

        /// <summary>
        /// Gets or sets the NPC's examine text.
        /// </summary>
        string Examine { get; set; }

        /// <summary>
        /// Gets or sets the NPC's attack speed in game ticks.
        /// </summary>
        int AttackSpeed { get; set; }

        /// <summary>
        /// Gets or sets the ID of the NPC's attack animation.
        /// </summary>
        int AttackAnimation { get; set; }

        /// <summary>
        /// Gets or sets the ID of the NPC's attack graphic effect.
        /// </summary>
        int AttackGraphic { get; set; }

        /// <summary>
        /// Gets or sets the ID of the NPC's defence animation.
        /// </summary>
        int DefenceAnimation { get; set; }

        /// <summary>
        /// Gets or sets the ID of the NPC's defence graphic effect.
        /// </summary>
        int DefenseGraphic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this NPC can be poisoned.
        /// </summary>
        bool Poisonable { get; set; }

        /// <summary>
        /// Gets or sets the ID of the NPC's death animation.
        /// </summary>
        int DeathAnimation { get; set; }

        /// <summary>
        /// Gets or sets the ID of the NPC's death graphic effect.
        /// </summary>
        int DeathGraphic { get; set; }

        /// <summary>
        /// Gets or sets the duration of the NPC's death animation in game ticks.
        /// </summary>
        int DeathTicks { get; set; }

        /// <summary>
        /// Gets or sets the collection of combat bonuses for this NPC.
        /// </summary>
        IBonuses Bonuses { get; set; }

        /// <summary>
        /// Gets or sets the Slayer level required to attack this NPC.
        /// </summary>
        int SlayerLevelRequired { get; set; }

        /// <summary>
        /// Gets or sets the time in game ticks it takes for this NPC to respawn after death.
        /// </summary>
        int RespawnTime { get; set; }

        /// <summary>
        /// Gets or sets the ID of the loot table used when this NPC is killed.
        /// </summary>
        int LootTableId { get; set; }
    }
}