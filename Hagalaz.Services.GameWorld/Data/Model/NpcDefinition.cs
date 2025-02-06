using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Data.Model
{
    /// <summary>
    /// Defines a single npc's defintions.
    /// </summary>
    public class NpcDefinition : NpcType, INpcDefinition
    {
        /// <summary>
        /// 
        /// </summary>
        private bool? _attackable;

        /// <summary>
        /// 
        /// </summary>
        private bool? _walksRandomly;

        /// <summary>
        /// The NPC's display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The NPC's examine text.
        /// </summary>
        public string Examine { get; set; }

        /// <summary>
        /// Contains max hitpoints of this npc.
        /// </summary>
        public int MaxLifePoints { get; set; }

        /// <summary>
        /// Contains npc respawn time.
        /// </summary>
        public int RespawnTime { get; set; }

        /// <summary>
        /// Contains npc reaction.
        /// </summary>
        public ReactionType ReactionType { get; set; }

        /// <summary>
        /// Contains npc walk type.
        /// </summary>
        public BoundsType BoundsType { get; set; }

        /// <summary>
        /// Contains this definition bonuses.
        /// </summary>
        public IBonuses Bonuses { get; set; }

        /// <summary>
        /// Contains boolean if this npc walks randomly.
        /// </summary>
        public bool WalksRandomly
        {
            get
            {
                if (_walksRandomly == null)
                {
                    return (WalkingProperties & 0x1) != 0 && (WalkingProperties & 0x2) != 0;
                }

                return _walksRandomly.Value;
            }
            set => _walksRandomly = value;
        }

        /// <summary>
        /// Contains npc attack level.
        /// </summary>
        public int MaxAttackLevel { get; set; }

        /// <summary>
        /// Contains npc strength level.
        /// </summary>
        public int MaxStrengthLevel { get; set; }

        /// <summary>
        /// Contains npc defence level.
        /// </summary>
        public int MaxDefenceLevel { get; set; }

        /// <summary>
        /// Contains npc ranged level.
        /// </summary>
        public int MaxRangedLevel { get; set; }

        /// <summary>
        /// Contains npc magic level.
        /// </summary>
        public int MaxMagicLevel { get; set; }

        /// <summary>
        /// Contains boolean if this npc is attackable.
        /// </summary>
        public bool Attackable
        {
            get
            {
                if (_attackable == null)
                {
                    return CombatLevel > 0;
                }

                return _attackable.Value;
            }
            set => _attackable = value;
        }

        /// <summary>
        /// Contains npc attack speed.
        /// </summary>
        public int AttackSpeed { get; set; }

        /// <summary>
        /// Contains npc attack animation.
        /// </summary>
        public int AttackAnimation { get; set; }

        /// <summary>
        /// Contains npc attack graphic.
        /// </summary>
        public int AttackGraphic { get; set; }

        /// <summary>
        /// Contains npc defence animation.
        /// </summary>
        public int DefenceAnimation { get; set; }

        /// <summary>
        /// Contains npc defence graphic.
        /// </summary>
        public int DefenseGraphic { get; set; }

        /// <summary>
        /// Contains npc death animation.
        /// </summary>
        public int DeathAnimation { get; set; }

        /// <summary>
        /// Contains npc death graphic.
        /// </summary>
        public int DeathGraphic { get; set; }

        /// <summary>
        /// Contains boolean if this npc is poisonable.
        /// </summary>
        public bool Poisonable { get; set; }

        /// <summary>
        /// Contains the death ticks.
        /// </summary>
        public int DeathTicks { get; set; }

        /// <summary>
        /// Contains the loot table id.
        /// </summary>
        public int LootTableId { get; set; }

        /// <summary>
        /// Contains the pick pocketing loot table Id.
        /// </summary>
        public int PickPocketingLootTableId { get; set; }

        /// <summary>
        /// Gets or sets the slayer level required.
        /// </summary>
        /// <value>
        /// The slayer level required.
        /// </value>
        public int SlayerLevelRequired { get; set; }

        /// <summary>
        /// Constructs the NPC definition.
        /// </summary>
        /// <param name="id">The npc owner index id.</param>
        public NpcDefinition(int id)
            : base(id)
        {
            Examine = "It's an npc.";
            ReactionType = ReactionType.Passive;
            BoundsType = BoundsType.Range;
            WalksRandomly = true;
            MaxLifePoints = 10;
            RespawnTime = 10;
            Bonuses = new Bonuses();
            MaxAttackLevel = 1;
            MaxStrengthLevel = 1;
            MaxDefenceLevel = 1;
            MaxRangedLevel = 1;
            MaxMagicLevel = 1;
            AttackSpeed = 8;
            AttackAnimation = 422;
            AttackGraphic = -1;
            DefenceAnimation = 404;
            DefenseGraphic = -1;
            DeathAnimation = 7197;
            DeathGraphic = -1;
            DeathTicks = 7;
            Poisonable = true;
            //this.Quests = new HashSet<short>();
            SlayerLevelRequired = 0;
        }

        public void AfterDecode()
        {
            DisplayName = Name;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => "NPCDefinition[id=" + Id + ",DisplayName=" + DisplayName + ",Reaction=" + ReactionType +
                                             ",BoundsType=" + BoundsType + ",CombatLevel=" + CombatLevel + ",Size=" + Size + "]";
    }
}