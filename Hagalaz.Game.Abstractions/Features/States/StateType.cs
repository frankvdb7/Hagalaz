namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines the various temporary states or effects that can be applied to a character or the game world.
    /// </summary>
    public enum StateType : int
    {
        /// <summary>
        /// A state indicating a Dragon Dagger is equipped, often enabling its special attack.
        /// </summary>
        DragonDaggerEquipped = 0,
        /// <summary>
        /// A state indicating an Abyssal Whip is equipped.
        /// </summary>
        AbyssalWhipEquipped = 1,
        /// <summary>
        /// A state indicating an Armadyl Godsword is equipped.
        /// </summary>
        ArmadylGodswordEquipped = 2,
        /// <summary>
        /// A state indicating a Bandos Godsword is equipped.
        /// </summary>
        BandosGodswordEquipped = 3,
        /// <summary>
        /// A state indicating a Saradomin Godsword is equipped.
        /// </summary>
        SaradominGodswordEquipped = 4,
        /// <summary>
        /// A state indicating a Zamorak Godsword is equipped.
        /// </summary>
        ZamorakGodswordEquipped = 5,
        /// <summary>
        /// The character has temporary immunity or resistance to poison.
        /// </summary>
        ResistPoison = 6,
        /// <summary>
        /// The character is frozen and cannot move.
        /// </summary>
        Frozen = 7,
        /// <summary>
        /// The character has temporary immunity to being frozen.
        /// </summary>
        ResistFreeze = 8,
        /// <summary>
        /// The character is afflicted with Nex's virus, causing damage over time.
        /// </summary>
        NexVirus = 9,
        /// <summary>
        /// The character is stunned and cannot perform actions.
        /// </summary>
        Stun = 10,
        /// <summary>
        /// The character is injured, which may affect their stats or actions.
        /// </summary>
        Injured = 11,
        /// <summary>
        /// The character is protecting one item upon death.
        /// </summary>
        ProtectOneItem = 12,
        /// <summary>
        /// The character is under the effect of the Turmoil curse, boosting stats.
        /// </summary>
        Turmoil = 13,
        /// <summary>
        /// The character is under the effect of the Sap Warrior curse, draining an opponent's stats.
        /// </summary>
        SapWarrior = 14,
        /// <summary>
        /// The character is under the effect of the Sap Ranger curse.
        /// </summary>
        SapRanger = 15,
        /// <summary>
        /// The character is under the effect of the Sap Mage curse.
        /// </summary>
        SapMager = 16,
        /// <summary>
        /// The character is under the effect of the Sap Spirit curse.
        /// </summary>
        SapSpirit = 17,
        /// <summary>
        /// The character is under the effect of the Leech Attack curse.
        /// </summary>
        LeechAttack = 18,
        /// <summary>
        /// The character is under the effect of the Leech Strength curse.
        /// </summary>
        LeechStrength = 19,
        /// <summary>
        /// The character is under the effect of the Leech Defence curse.
        /// </summary>
        LeechDefence = 20,
        /// <summary>
        /// The character is under the effect of the Leech Ranged curse.
        /// </summary>
        LeechRanged = 21,
        /// <summary>
        /// The character is under the effect of the Leech Magic curse.
        /// </summary>
        LeechMagic = 22,
        /// <summary>
        /// The character is under the effect of the Leech Energy curse.
        /// </summary>
        LeechEnergy = 23,
        /// <summary>
        /// The character is under the effect of the Leech Special Attack curse.
        /// </summary>
        LeechSpecial = 24,
        /// <summary>
        /// The character is currently in the process of eating food.
        /// </summary>
        Eating = 25,
        /// <summary>
        /// The character has the Vengeance spell active, which reflects damage.
        /// </summary>
        Vengeance = 27,
        /// <summary>
        /// The character is temporarily immune to the Vengeance spell.
        /// </summary>
        VengeanceImmunity = 28,
        /// <summary>
        /// The character is temporarily unable to cast the Vengeance spell.
        /// </summary>
        CantCastVengeance = 29,
        /// <summary>
        /// A state indicating a Vesta's Longsword is equipped.
        /// </summary>
        VestaLongswordEquipped = 30,
        /// <summary>
        /// A state indicating a Statius's Warhammer is equipped.
        /// </summary>
        StatiusWarhammerEquipped = 31,
        /// <summary>
        /// A state indicating a Korasi's Sword is equipped.
        /// </summary>
        KorasiEquipped = 32,
        /// <summary>
        /// The character is currently in the process of drinking a potion.
        /// </summary>
        Drinking = 33,
        /// <summary>
        /// The character is slowed by a miasmic spell effect.
        /// </summary>
        MiasmicSlow = 35,
        /// <summary>
        /// The character is temporarily immune to miasmic slowing effects.
        /// </summary>
        MiasmicSlowImmunity = 36,
        /// <summary>
        /// The character is under the effect of an anti-dragonfire potion.
        /// </summary>
        AntiDragonfirePotion = 37,
        /// <summary>
        /// The character has an anti-dragonfire shield equipped.
        /// </summary>
        AntiDragonfireShield = 38,
        /// <summary>
        /// A state indicating enchanted opal bolts are equipped.
        /// </summary>
        EnchantedOpalBoltsEquipped = 39,
        /// <summary>
        /// A state indicating enchanted diamond bolts are equipped.
        /// </summary>
        EnchantedDiamondBoltsEquipped = 40,
        /// <summary>
        /// A state indicating enchanted dragonstone bolts are equipped.
        /// </summary>
        EnchantedDragonstoneBoltsEquiped = 41,
        /// <summary>
        /// A state indicating enchanted onyx bolts are equipped.
        /// </summary>
        EnchantedOnyxBoltsEquiped = 42,
        /// <summary>
        /// A state indicating Morrigan's thrown axe is equipped.
        /// </summary>
        MorrigansThrownAxeEquiped = 43,
        /// <summary>
        /// A state indicating a Dark bow is equipped.
        /// </summary>
        DarkBowEquiped = 44,
        /// <summary>
        /// A state indicating dragon arrows are equipped.
        /// </summary>
        DragonArrowsEquiped = 45,
        /// <summary>
        /// A state indicating Zanik's crossbow is equipped.
        /// </summary>
        ZanikCrossbowEquiped = 46,
        /// <summary>
        /// A generic state indicating a crossbow is equipped.
        /// </summary>
        CrossbowEquiped = 47,
        /// <summary>
        /// The character is under the effect of the Berserker prayer.
        /// </summary>
        Berserker = 48,
        /// <summary>
        /// The character is under the effect of the Rapid Restore prayer.
        /// </summary>
        RapidRestore = 49,
        /// <summary>
        /// The character is under the effect of the Rapid Heal prayer.
        /// </summary>
        RapidHeal = 50,
        /// <summary>
        /// The character is under the effect of the Rapid Renewal prayer.
        /// </summary>
        RapidRenewal = 51,
        /// <summary>
        /// The character has the Redemption prayer active, which heals upon low health.
        /// </summary>
        Redemption = 52,
        /// <summary>
        /// The character has the Retribution prayer active, which damages nearby enemies upon death.
        /// </summary>
        Retribution = 53,
        /// <summary>
        /// The character is currently in the process of burying bones.
        /// </summary>
        BuryingBones = 54,
        /// <summary>
        /// The character is currently in the process of teleporting.
        /// </summary>
        Teleporting = 55,
        /// <summary>
        /// The character is skulled, typically from attacking another player in a PvP area.
        /// </summary>
        DefaultSkulled = 56,
        /// <summary>
        /// The character is under the effect of a super anti-dragonfire potion.
        /// </summary>
        SuperAntiDragonfirePotion = 57,
        /// <summary>
        /// A state indicating a Magic shortbow is equipped.
        /// </summary>
        MagicShortBowEquiped = 58,
        /// <summary>
        /// A state indicating a full set of Void Knight melee armour is equipped.
        /// </summary>
        VoidMeleeEquiped = 59,
        /// <summary>
        /// A state indicating a full set of Void Knight magic armour is equipped.
        /// </summary>
        VoidMagicEquiped = 60,
        /// <summary>
        /// A state indicating a full set of Void Knight ranged armour is equipped.
        /// </summary>
        VoidRangedEquiped = 61,
        //CustomizingCape = 62,
        /// <summary>
        /// The character has placed a dwarf multi-cannon.
        /// </summary>
        CannonPlaced = 63,
        /// <summary>
        /// The character is currently resting to restore run energy faster.
        /// </summary>
        Resting = 64,
        /// <summary>
        /// The character is listening to a musician to restore run energy.
        /// </summary>
        ListeningToMusician = 65,
        /// <summary>
        /// A state indicating a Dragon longsword is equipped.
        /// </summary>
        DragonLongSwordEquiped = 66,
        /// <summary>
        /// A state indicating a Dragon mace is equipped.
        /// </summary>
        DragonMaceEquiped = 67,
        /// <summary>
        /// A state indicating a Dragon scimitar is equipped.
        /// </summary>
        DragonScimitarEquiped = 68,
        //InGodWars = 69,
        /// <summary>
        /// The character has a rope deployed at the God Wars Dungeon entrance hole.
        /// </summary>
        HasGodWarsHoleRope = 70,
        /// <summary>
        /// The character is currently interacting with a bank.
        /// </summary>
        Banking = 71,
        /// <summary>
        /// A state indicating a Granite maul is equipped.
        /// </summary>
        GraniteMaulEquiped = 72,
        /// <summary>
        /// The character is benefiting from the Staff of Light's special effect, which can save runes.
        /// </summary>
        StaffOfLightSpecialEffect = 73,
        /// <summary>
        /// The character is currently in the process of thieving from a stall.
        /// </summary>
        ThievingStall = 74,
        /// <summary>
        /// The character is currently in the process of pickpocketing an NPC.
        /// </summary>
        ThievingNpc = 75,
        /// <summary>
        /// The character is tele-blocked and cannot use most teleportation methods.
        /// </summary>
        TeleBlocked = 76,
        /// <summary>
        /// A generic state indicating that bolts are equipped.
        /// </summary>
        BoltsEquiped = 77,
        /// <summary>
        /// A generic state indicating that a bow is equipped.
        /// </summary>
        BowEquiped = 78,
        /// <summary>
        /// A generic state indicating that arrows are equipped.
        /// </summary>
        ArrowsEquiped = 79,
        /// <summary>
        /// The character is currently in the process of casting an alchemy spell.
        /// </summary>
        Alching = 80,
        /// <summary>
        /// A state indicating a Bronze defender is equipped.
        /// </summary>
        BronzeDefender = 81,
        /// <summary>
        /// A state indicating an Iron defender is equipped.
        /// </summary>
        IronDefender = 82,
        /// <summary>
        /// A state indicating a Steel defender is equipped.
        /// </summary>
        SteelDefender = 83,
        /// <summary>
        /// A state indicating a Black defender is equipped.
        /// </summary>
        BlackDefender = 84,
        /// <summary>
        /// A state indicating a Mithril defender is equipped.
        /// </summary>
        MithrilDefender = 85,
        /// <summary>
        /// A state indicating an Adamant defender is equipped.
        /// </summary>
        AdamantDefender = 86,
        /// <summary>
        /// A state indicating a Rune defender is equipped.
        /// </summary>
        RuneDefender = 87,
        /// <summary>
        /// A state indicating a Dragon defender is equipped.
        /// </summary>
        DragonDefender = 88,
        /// <summary>
        /// The character is currently between doors in the Barrows crypt.
        /// </summary>
        BarrowsBetweenDoors = 89,
        /// <summary>
        /// The character has opened the final chest in the Barrows minigame.
        /// </summary>
        BarrowsOpenedChest = 90,
        /// <summary>
        /// A state indicating a Ring of Wealth is equipped.
        /// </summary>
        RingOfWealthEquiped = 91,
        /// <summary>
        /// The character is under the effect of Dharok's Barrows set, increasing damage at low health.
        /// </summary>
        DharokWretchedStrength = 92,
        /// <summary>
        /// The character is under the effect of Guthan's Barrows set, which has a chance to heal on hit.
        /// </summary>
        GuthanInfestation = 93,
        /// <summary>
        /// The character has temporary immunity to melee attacks.
        /// </summary>
        MeleeImmunity = 94,
        /// <summary>
        /// The character is under the effect of an Overload potion, boosting combat stats.
        /// </summary>
        OverloadEffect = 96,
        /// <summary>
        /// The character is under the effect of Ahrim's Barrows set, which can drain opponent's Strength.
        /// </summary>
        AhrimBlight = 97,
        /// <summary>
        /// The character is under the effect of Verac's Barrows set, which can ignore armour and prayer.
        /// </summary>
        VeracDefile = 98,
        /// <summary>
        /// The character is under the effect of Torag's Barrows set, which can drain opponent's run energy.
        /// </summary>
        ToragCorrupt = 99,
        /// <summary>
        /// The character is under the effect of Karil's Barrows set, which can drain opponent's Agility.
        /// </summary>
        KarilTaint = 100,
        /// <summary>
        /// The character is under the effect of Akrisae's Barrows set, which can drain opponent's prayer.
        /// </summary>
        AkrisaeDoom = 101,
        /// <summary>
        /// The character has recently prayed at the Armadyl altar in God Wars Dungeon.
        /// </summary>
        ArmadylAltarPrayed = 102,
        /// <summary>
        /// The character has recently prayed at the Bandos altar in God Wars Dungeon.
        /// </summary>
        BandosAltarPrayed = 103,
        /// <summary>
        /// The character has recently prayed at the Saradomin altar in God Wars Dungeon.
        /// </summary>
        SaradominAltarPrayed = 104,
        /// <summary>
        /// The character has recently prayed at the Zamorak altar in God Wars Dungeon.
        /// </summary>
        ZamorakAltarPrayed = 105,
        /// <summary>
        /// A state indicating a Mithril grapple is equipped.
        /// </summary>
        MithGrappleEquiped = 106,
        /// <summary>
        /// The character has a rope deployed at the first rock in the Saradomin area of God Wars Dungeon.
        /// </summary>
        HasSaradominFirstRockRope = 107,
        /// <summary>
        /// The character has a rope deployed at the last rock in the Saradomin area of God Wars Dungeon.
        /// </summary>
        HasSaradominLastRockRope = 108,
        /// <summary>
        /// The character is under the effect of a special attack recovery potion.
        /// </summary>
        RecoverSpecialPotion = 109,
        /// <summary>
        /// A warning state indicating that an anti-dragonfire potion is about to expire.
        /// </summary>
        AntiDragonfirePotionWarning = 110,
        /// <summary>
        /// The character has been frozen by a Glacor.
        /// </summary>
        GlacorFrozen = 111,
        /// <summary>
        /// A state indicating an Ava's attractor is equipped.
        /// </summary>
        AvasAttractorEquiped = 112,
        /// <summary>
        /// A state indicating an Ava's accumulator is equipped.
        /// </summary>
        AvasAccumulatorEquiped = 113,
        /// <summary>
        /// A state indicating an Ava's alerter is equipped.
        /// </summary>
        AvasAlerterEquiped = 114,
        /// <summary>
        /// The character is currently viewing the "sell" screen of a shop.
        /// </summary>
        ShopSellScreen = 115,
        /// <summary>
        /// The character has unlocked the Bandit Camp lodestone.
        /// </summary>
        LodestoneBanditCamp = 116,
        /// <summary>
        /// The character has unlocked the Lunar Isle lodestone.
        /// </summary>
        LodestoneLunarIsle = 117,
        /// <summary>
        /// The character has unlocked the Al Kharid lodestone.
        /// </summary>
        LodestoneAlkharid = 118,
        /// <summary>
        /// The character has unlocked the Ardougne lodestone.
        /// </summary>
        LodestoneArdougne = 119,
        /// <summary>
        /// The character has unlocked the Burthorpe lodestone.
        /// </summary>
        LodestoneBurthorpe = 120,
        /// <summary>
        /// The character has unlocked the Catherby lodestone.
        /// </summary>
        LodestoneCatherby = 121,
        /// <summary>
        /// The character has unlocked the Draynor lodestone.
        /// </summary>
        LodestoneDraynor = 122,
        /// <summary>
        /// The character has unlocked the Edgeville lodestone.
        /// </summary>
        LodestoneEdgeville = 123,
        /// <summary>
        /// The character has unlocked the Falador lodestone.
        /// </summary>
        LodestoneFalador = 124,
        /// <summary>
        /// The character has unlocked the Lumbridge lodestone.
        /// </summary>
        LodestoneLumbridge = 125,
        /// <summary>
        /// The character has unlocked the Port Sarim lodestone.
        /// </summary>
        LodestonePortSarim = 126,
        /// <summary>
        /// The character has unlocked the Seers' Village lodestone.
        /// </summary>
        LodestoneSeersVillage = 127,
        /// <summary>
        /// The character has unlocked the Taverley lodestone.
        /// </summary>
        LodestoneTaverley = 128,
        /// <summary>
        /// The character has unlocked the Varrock lodestone.
        /// </summary>
        LodestoneVarrock = 129,
        /// <summary>
        /// The character has unlocked the Yanille lodestone.
        /// </summary>
        LodestoneYanille = 130,
        /// <summary>
        /// The character is under the effect of the Charge spell.
        /// </summary>
        Charge = 131,
        /// <summary>
        /// The character is temporarily unable to cast the Charge spell.
        /// </summary>
        CantCastCharge = 132,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course log balance.
        /// </summary>
        GnomeCourseLogBalance = 133,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course first obstacle net.
        /// </summary>
        GnomeCourseFirstObstacleNet = 134,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course first tree branch.
        /// </summary>
        GnomeCourseFirstTreeBranch = 135,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course balancing rope.
        /// </summary>
        GnomeCourseBalancingRope = 136,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course tree branch down.
        /// </summary>
        GnomeCourseTreeBranchDown = 137,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course second obstacle net.
        /// </summary>
        GnomeCourseSecondObstacleNet = 138,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course obstacle pipe.
        /// </summary>
        GnomeCourseObstaclePipe = 139,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course tree.
        /// </summary>
        GnomeCourseTree = 140,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course signpost.
        /// </summary>
        GnomeCourseSignpost = 141,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course pole.
        /// </summary>
        GnomeCoursePole = 142,
        /// <summary>
        /// A state used for tracking progress on the Gnome Agility Course barrier.
        /// </summary>
        GnomeCourseBarrier = 143,

        /// <summary>
        /// A state indicating the NPC is a dragon, which may affect its weaknesses or drops.
        /// </summary>
        NpcTypeDragon=100000,
        /// <summary>
        /// A state indicating the NPC is undead, which may affect its weaknesses or drops.
        /// </summary>
        NpcTypeUndead=100001,
        /// <summary>
        /// A state indicating the NPC is a demon, which may affect its weaknesses or drops.
        /// </summary>
        NpcTypeDemon=100002
        
    }
}
