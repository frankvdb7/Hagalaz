namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// States are used mainly for global scripts.
    /// </summary>
    public enum StateType : int
    {
        /// <summary>
        /// 
        /// </summary>
        DragonDaggerEquipped = 0,
        /// <summary>
        /// 
        /// </summary>
        AbyssalWhipEquipped = 1,
        /// <summary>
        /// 
        /// </summary>
        ArmadylGodswordEquipped = 2,
        /// <summary>
        /// 
        /// </summary>
        BandosGodswordEquipped = 3,
        /// <summary>
        /// 
        /// </summary>
        SaradominGodswordEquipped = 4,
        /// <summary>
        /// 
        /// </summary>
        ZamorakGodswordEquipped = 5,
        /// <summary>
        /// 
        /// </summary>
        ResistPoison = 6,
        /// <summary>
        /// 
        /// </summary>
        Frozen = 7,
        /// <summary>
        /// 
        /// </summary>
        ResistFreeze = 8,
        /// <summary>
        /// 
        /// </summary>
        NexVirus = 9,
        /// <summary>
        /// 
        /// </summary>
        Stun = 10,
        /// <summary>
        /// 
        /// </summary>
        Injured = 11,
        /// <summary>
        /// 
        /// </summary>
        ProtectOneItem = 12,
        /// <summary>
        /// 
        /// </summary>
        Turmoil = 13,
        /// <summary>
        /// 
        /// </summary>
        SapWarrior = 14,
        /// <summary>
        /// 
        /// </summary>
        SapRanger = 15,
        /// <summary>
        /// 
        /// </summary>
        SapMager = 16,
        /// <summary>
        /// 
        /// </summary>
        SapSpirit = 17,
        /// <summary>
        /// 
        /// </summary>
        LeechAttack = 18,
        /// <summary>
        /// 
        /// </summary>
        LeechStrength = 19,
        /// <summary>
        /// 
        /// </summary>
        LeechDefence = 20,
        /// <summary>
        /// 
        /// </summary>
        LeechRanged = 21,
        /// <summary>
        /// 
        /// </summary>
        LeechMagic = 22,
        /// <summary>
        /// 
        /// </summary>
        LeechEnergy = 23,
        /// <summary>
        /// 
        /// </summary>
        LeechSpecial = 24,
        /// <summary>
        /// 
        /// </summary>
        Eating = 25,
        /// <summary>
        /// 
        /// </summary>
        Vengeance = 27,
        /// <summary>
        /// 
        /// </summary>
        VengeanceImmunity = 28,
        /// <summary>
        /// 
        /// </summary>
        CantCastVengeance = 29,
        /// <summary>
        /// 
        /// </summary>
        VestaLongswordEquipped = 30,
        /// <summary>
        /// 
        /// </summary>
        StatiusWarhammerEquipped = 31,
        /// <summary>
        /// 
        /// </summary>
        KorasiEquipped = 32,
        /// <summary>
        /// 
        /// </summary>
        Drinking = 33,
        /// <summary>
        /// 
        /// </summary>
        MiasmicSlow = 35,
        /// <summary>
        /// 
        /// </summary>
        MiasmicSlowImmunity = 36,
        /// <summary>
        /// 
        /// </summary>
        AntiDragonfirePotion = 37,
        /// <summary>
        /// 
        /// </summary>
        AntiDragonfireShield = 38,
        /// <summary>
        /// 
        /// </summary>
        EnchantedOpalBoltsEquipped = 39,
        /// <summary>
        /// 
        /// </summary>
        EnchantedDiamondBoltsEquipped = 40,
        /// <summary>
        /// 
        /// </summary>
        EnchantedDragonstoneBoltsEquiped = 41,
        /// <summary>
        /// 
        /// </summary>
        EnchantedOnyxBoltsEquiped = 42,
        /// <summary>
        /// 
        /// </summary>
        MorrigansThrownAxeEquiped = 43,
        /// <summary>
        /// 
        /// </summary>
        DarkBowEquiped = 44,
        /// <summary>
        /// 
        /// </summary>
        DragonArrowsEquiped = 45,
        /// <summary>
        /// 
        /// </summary>
        ZanikCrossbowEquiped = 46,
        /// <summary>
        /// 
        /// </summary>
        CrossbowEquiped = 47,
        /// <summary>
        /// 
        /// </summary>
        Berserker = 48,
        /// <summary>
        /// 
        /// </summary>
        RapidRestore = 49,
        /// <summary>
        /// 
        /// </summary>
        RapidHeal = 50,
        /// <summary>
        /// 
        /// </summary>
        RapidRenewal = 51,
        /// <summary>
        /// 
        /// </summary>
        Redemption = 52,
        /// <summary>
        /// 
        /// </summary>
        Retribution = 53,
        /// <summary>
        /// 
        /// </summary>
        BuryingBones = 54,
        /// <summary>
        /// 
        /// </summary>
        Teleporting = 55,
        /// <summary>
        /// 
        /// </summary>
        DefaultSkulled = 56,
        /// <summary>
        /// 
        /// </summary>
        SuperAntiDragonfirePotion = 57,
        /// <summary>
        /// 
        /// </summary>
        MagicShortBowEquiped = 58,
        /// <summary>
        /// 
        /// </summary>
        VoidMeleeEquiped = 59,
        /// <summary>
        /// 
        /// </summary>
        VoidMagicEquiped = 60,
        /// <summary>
        /// 
        /// </summary>
        VoidRangedEquiped = 61,
        //CustomizingCape = 62,
        /// <summary>
        /// 
        /// </summary>
        CannonPlaced = 63,
        /// <summary>
        /// 
        /// </summary>
        Resting = 64,
        /// <summary>
        /// 
        /// </summary>
        ListeningToMusician = 65,
        /// <summary>
        /// 
        /// </summary>
        DragonLongSwordEquiped = 66,
        /// <summary>
        /// 
        /// </summary>
        DragonMaceEquiped = 67,
        /// <summary>
        /// 
        /// </summary>
        DragonScimitarEquiped = 68,
        //InGodWars = 69,
        /// <summary>
        /// 
        /// </summary>
        HasGodWarsHoleRope = 70,
        /// <summary>
        /// 
        /// </summary>
        Banking = 71,
        /// <summary>
        /// 
        /// </summary>
        GraniteMaulEquiped = 72,
        /// <summary>
        /// 
        /// </summary>
        StaffOfLightSpecialEffect = 73,
        /// <summary>
        /// 
        /// </summary>
        ThievingStall = 74,
        /// <summary>
        /// 
        /// </summary>
        ThievingNpc = 75,
        /// <summary>
        /// 
        /// </summary>
        TeleBlocked = 76,
        /// <summary>
        /// 
        /// </summary>
        BoltsEquiped = 77,
        /// <summary>
        /// 
        /// </summary>
        BowEquiped = 78,
        /// <summary>
        /// 
        /// </summary>
        ArrowsEquiped = 79,
        /// <summary>
        /// 
        /// </summary>
        Alching = 80,
        /// <summary>
        /// 
        /// </summary>
        BronzeDefender = 81,
        /// <summary>
        /// 
        /// </summary>
        IronDefender = 82,
        /// <summary>
        /// 
        /// </summary>
        SteelDefender = 83,
        /// <summary>
        /// 
        /// </summary>
        BlackDefender = 84,
        /// <summary>
        /// 
        /// </summary>
        MithrilDefender = 85,
        /// <summary>
        /// 
        /// </summary>
        AdamantDefender = 86,
        /// <summary>
        /// 
        /// </summary>
        RuneDefender = 87,
        /// <summary>
        /// 
        /// </summary>
        DragonDefender = 88,
        /// <summary>
        /// 
        /// </summary>
        BarrowsBetweenDoors = 89,
        /// <summary>
        /// 
        /// </summary>
        BarrowsOpenedChest = 90,
        /// <summary>
        /// 
        /// </summary>
        RingOfWealthEquiped = 91,
        /// <summary>
        /// 
        /// </summary>
        DharokWretchedStrength = 92,
        /// <summary>
        /// 
        /// </summary>
        GuthanInfestation = 93,
        /// <summary>
        /// 
        /// </summary>
        MeleeImmunity = 94,
        /// <summary>
        /// 
        /// </summary>
        OverloadEffect = 96,
        /// <summary>
        /// 
        /// </summary>
        AhrimBlight = 97,
        /// <summary>
        /// 
        /// </summary>
        VeracDefile = 98,
        /// <summary>
        /// 
        /// </summary>
        ToragCorrupt = 99,
        /// <summary>
        ///
        /// </summary>
        KarilTaint = 100,
        /// <summary>
        /// 
        /// </summary>
        AkrisaeDoom = 101,
        /// <summary>
        /// 
        /// </summary>
        ArmadylAltarPrayed = 102,
        /// <summary>
        /// 
        /// </summary>
        BandosAltarPrayed = 103,
        /// <summary>
        /// 
        /// </summary>
        SaradominAltarPrayed = 104,
        /// <summary>
        /// 
        /// </summary>
        ZamorakAltarPrayed = 105,
        /// <summary>
        /// 
        /// </summary>
        MithGrappleEquiped = 106,
        /// <summary>
        /// 
        /// </summary>
        HasSaradominFirstRockRope = 107,
        /// <summary>
        /// 
        /// </summary>
        HasSaradominLastRockRope = 108,
        /// <summary>
        /// 
        /// </summary>
        RecoverSpecialPotion = 109,
        /// <summary>
        /// 
        /// </summary>
        AntiDragonfirePotionWarning = 110,
        /// <summary>
        ///
        /// </summary>
        GlacorFrozen = 111,
        /// <summary>
        ///
        /// </summary>
        AvasAttractorEquiped = 112,
        /// <summary>
        /// 
        /// </summary>
        AvasAccumulatorEquiped = 113,
        /// <summary>
        /// 
        /// </summary>
        AvasAlerterEquiped = 114,
        /// <summary>
        /// 
        /// </summary>
        ShopSellScreen = 115,
        /// <summary>
        /// The bandit camp lodestone
        /// </summary>
        LodestoneBanditCamp = 116,
        /// <summary>
        /// The lunar isle lodestone
        /// </summary>
        LodestoneLunarIsle = 117,
        /// <summary>
        /// The alkharid lodestone
        /// </summary>
        LodestoneAlkharid = 118,
        /// <summary>
        /// The ardougne lodestone
        /// </summary>
        LodestoneArdougne = 119,
        /// <summary>
        /// The burthorpe lodestone
        /// </summary>
        LodestoneBurthorpe = 120,
        /// <summary>
        /// The catherby lodestone
        /// </summary>
        LodestoneCatherby = 121,
        /// <summary>
        /// The draynor lodestone
        /// </summary>
        LodestoneDraynor = 122,
        /// <summary>
        /// The edgeville lodestone
        /// </summary>
        LodestoneEdgeville = 123,
        /// <summary>
        /// The falador lodestone
        /// </summary>
        LodestoneFalador = 124,
        /// <summary>
        /// The lumbridge lodestone
        /// </summary>
        LodestoneLumbridge = 125,
        /// <summary>
        /// The port sarim lodestone
        /// </summary>
        LodestonePortSarim = 126,
        /// <summary>
        /// The seers village lodestone
        /// </summary>
        LodestoneSeersVillage = 127,
        /// <summary>
        /// The taverley lodestone
        /// </summary>
        LodestoneTaverley = 128,
        /// <summary>
        /// The varrock lodestone
        /// </summary>
        LodestoneVarrock = 129,
        /// <summary>
        /// The yanille lodestone
        /// </summary>
        LodestoneYanille = 130,
        /// <summary>
        /// The charge spell casted
        /// </summary>
        Charge = 131,
        /// <summary>
        /// The cant cast charge
        /// </summary>
        CantCastCharge = 132,
        /// <summary>
        /// The gnome course log balance
        /// </summary>
        GnomeCourseLogBalance = 133,
        /// <summary>
        /// The gnome course first obstacle net
        /// </summary>
        GnomeCourseFirstObstacleNet = 134,
        /// <summary>
        /// The gnome course first tree branch
        /// </summary>
        GnomeCourseFirstTreeBranch = 135,
        /// <summary>
        /// The gnome course balancing rope
        /// </summary>
        GnomeCourseBalancingRope = 136,
        /// <summary>
        /// The gnome course tree branch down
        /// </summary>
        GnomeCourseTreeBranchDown = 137,
        /// <summary>
        /// The gnome course second obstacle net
        /// </summary>
        GnomeCourseSecondObstacleNet = 138,
        /// <summary>
        /// The gnome course obstacle pipe
        /// </summary>
        GnomeCourseObstaclePipe = 139,
        /// <summary>
        /// The gnome course tree
        /// </summary>
        GnomeCourseTree = 140,
        /// <summary>
        /// The gnome course signpost
        /// </summary>
        GnomeCourseSignpost = 141,
        /// <summary>
        /// The gnome course pole
        /// </summary>
        GnomeCoursePole = 142,
        /// <summary>
        /// The gnome course barrier
        /// </summary>
        GnomeCourseBarrier = 143,

        /// <summary>
        /// The NPC type dragon
        /// </summary>
        NpcTypeDragon=100000,
        /// <summary>
        /// The NPC type undead
        /// </summary>
        NpcTypeUndead=100001,
        /// <summary>
        /// The NPC type demon
        /// </summary>
        NpcTypeDemon=100002
        
    }
}
