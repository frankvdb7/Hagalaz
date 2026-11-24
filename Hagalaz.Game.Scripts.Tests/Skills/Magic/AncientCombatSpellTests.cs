using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Skills.Combat.Magic;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Magic;

/// <summary>
///     Pins the observable behaviour of the Ancient (Ice, Shadow, Smoke, Blood, Miasmic) combat spells.
///     Expectations are literal values taken from the original per-family implementations.
/// </summary>
[TestClass]
public sealed class AncientCombatSpellTests
{
    private const int MagicSkill = StatisticsConstants.Magic;

    // ---------- Shared behaviour ----------

    [TestMethod]
    [DataRow("ice", 0, 1978, 34.0, 180)]
    [DataRow("ice", 1, 1979, 40.0, 220)]
    [DataRow("ice", 2, 1978, 46.0, 260)]
    [DataRow("ice", 3, 1979, 52.0, 300)]
    [DataRow("shadow", 0, 1978, 31.0, 140)]
    [DataRow("shadow", 1, 1979, 37.0, 180)]
    [DataRow("shadow", 2, 1978, 43.0, 240)]
    [DataRow("shadow", 3, 1979, 49.0, 280)]
    [DataRow("smoke", 0, 1978, 30.0, 130)]
    [DataRow("smoke", 1, 1979, 36.0, 170)]
    [DataRow("smoke", 2, 1978, 42.0, 264)]
    [DataRow("smoke", 3, 1979, 48.0, 270)]
    [DataRow("blood", 0, 1978, 33.0, 150)]
    [DataRow("blood", 1, 1979, 39.0, 210)]
    [DataRow("blood", 2, 1978, 45.0, 250)]
    [DataRow("blood", 3, 1979, 51.0, 290)]
    [DataRow("miasmic", 0, 10513, 36.0, 180)]
    [DataRow("miasmic", 1, 10516, 42.0, 240)]
    [DataRow("miasmic", 2, 10524, 48.0, 280)]
    [DataRow("miasmic", 3, 10518, 54.0, 352)]
    public void Cast_AnimatesGrantsExperienceAndAttacksVictimWithFullMagicAtDistanceDelay(
        string family, int type, int animation, double experience, int maxDamage)
    {
        var scenario = new Scenario();

        Create(family, type, scenario).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.AreEqual(animation, scenario.CasterAnimations.Single());
        scenario.Caster.Statistics.Received(1).AddExperience(MagicSkill, experience);
        var attack = scenario.Attacks.Single();
        Assert.AreEqual(scenario.Victim.Handle, attack.Target);
        Assert.AreEqual(maxDamage, attack.MaxDamage);
        Assert.AreEqual(maxDamage, attack.Damage);
        Assert.AreEqual(DamageType.FullMagic, attack.DamageType);
        Assert.AreEqual(51 + (2 + 0) * 5, attack.Delay);
        Assert.AreEqual(experience, Create(family, type, scenario).GetMagicExperience());
    }

    [TestMethod]
    [DataRow("ice", 1)]
    [DataRow("ice", 3)]
    [DataRow("shadow", 1)]
    [DataRow("shadow", 3)]
    [DataRow("smoke", 1)]
    [DataRow("smoke", 3)]
    [DataRow("blood", 1)]
    [DataRow("blood", 3)]
    [DataRow("miasmic", 1)]
    [DataRow("miasmic", 3)]
    public void MultiHitCast_InMultiCombatAlsoHitsAdjacentCreaturesAndSkullsThem(string family, int type)
    {
        var scenario = new Scenario();
        var neighbour = scenario.AddBystander(victimX: 12, x: 13, y: 10);
        var distant = scenario.AddBystander(victimX: 12, x: 15, y: 10);

        Create(family, type, scenario).PerformAttack(scenario.Caster, scenario.Victim);

        CollectionAssert.AreEquivalent(
            new object[] { scenario.Victim.Handle, neighbour.Handle },
            scenario.Attacks.Select(a => a.Target).ToArray());
        Assert.IsFalse(scenario.Attacks.Any(a => a.Target.Equals(distant.Handle)));
        Assert.AreEqual(51 + (3 + 0) * 5, scenario.Attacks.Single(a => a.Target.Equals(neighbour.Handle)).Delay);
        if (family == "blood")
        {
            scenario.CharacterCombat.DidNotReceive().CheckSkullConditions(Arg.Any<ICreature>());
        }
        else
        {
            scenario.CharacterCombat.Received(1).CheckSkullConditions(neighbour);
            scenario.CharacterCombat.DidNotReceive().CheckSkullConditions(scenario.Victim);
        }
    }

    [TestMethod]
    [DataRow("ice", 0)]
    [DataRow("ice", 2)]
    [DataRow("shadow", 0)]
    [DataRow("smoke", 2)]
    [DataRow("blood", 0)]
    [DataRow("miasmic", 2)]
    public void SingleTargetCast_NeverHitsAdjacentCreatures(string family, int type)
    {
        var scenario = new Scenario();
        scenario.AddBystander(victimX: 12, x: 13, y: 10);

        Create(family, type, scenario).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.AreEqual(scenario.Victim.Handle, scenario.Attacks.Single().Target);
    }

    [TestMethod]
    public void MultiHitCast_OutsideMultiCombatOnlyHitsVictim()
    {
        var scenario = new Scenario { MultiCombat = false };
        scenario.AddBystander(victimX: 12, x: 13, y: 10);

        new AncientIceCombatSpell(1, Definition("ice", 1), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.AreEqual(scenario.Victim.Handle, scenario.Attacks.Single().Target);
    }

    [TestMethod]
    public void Spell_ReportsStandardCastingProperties()
    {
        var scenario = new Scenario();
        var caster = scenario.Caster;
        var spell = new AncientBloodCombatSpell(2, Definition("blood", 2), scenario.Projectiles);

        Assert.AreEqual(5, spell.GetCastingSpeed(caster));
        Assert.AreEqual(8, spell.GetCombatDistance(caster));
        Assert.IsTrue(spell.CanAttack(caster, scenario.Victim));

        spell.OnAutoCastingActivation(caster);
        caster.Configurations.Received(1).SendStandardConfiguration(108, 83);
        spell.OnAutoCastingDeactivation(caster);
        caster.Configurations.Received(1).SendStandardConfiguration(108, -1);
    }

    [TestMethod]
    [DataRow("ice", 0, 58, new[] { RuneType.Chaos, RuneType.Death, RuneType.Water }, new[] { 2, 2, 2 })]
    [DataRow("ice", 3, 94, new[] { RuneType.Death, RuneType.Blood, RuneType.Water }, new[] { 4, 2, 6 })]
    [DataRow("blood", 3, 92, new[] { RuneType.Death, RuneType.Blood, RuneType.Soul }, new[] { 4, 2, 1 })]
    public void Requirements_CheckAndRemoveConfiguredLevelAndRunes(
        string family, int type, int level, RuneType[] runes, int[] amounts)
    {
        var scenario = new Scenario();
        var spell = Create(family, type, scenario);
        scenario.Caster.Magic.CheckMagicLevel(level).Returns(true);
        scenario.Caster.Magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(true);

        Assert.IsTrue(spell.CheckRequirements(scenario.Caster));
        scenario.Caster.Magic.Received().CheckRunes(
            Arg.Is<RuneType[]>(r => r.SequenceEqual(runes)), Arg.Is<int[]>(a => a.SequenceEqual(amounts)));

        spell.RemoveRequirements(scenario.Caster);
        scenario.Caster.Magic.Received(1).RemoveRunes(
            Arg.Is<RuneType[]>(r => r.SequenceEqual(runes)), Arg.Is<int[]>(a => a.SequenceEqual(amounts)));
    }

    // ---------- Ice ----------

    [TestMethod]
    [DataRow(0, 360)]
    [DataRow(1, 366)]
    [DataRow(3, 368)]
    public void Ice_SendsProjectile(int type, int projectile)
    {
        var scenario = new Scenario();

        new AncientIceCombatSpell(type, Definition("ice", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        CollectionAssert.AreEqual(new[] { projectile }, scenario.ProjectileGraphics);
    }

    [TestMethod]
    public void Ice_Rush_HasNoProjectileButCastsSelfGraphic()
    {
        var scenario = new Scenario();

        new AncientIceCombatSpell(2, Definition("ice", 2), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.IsEmpty(scenario.ProjectileGraphics);
        Assert.IsTrue(scenario.CasterGraphics.Any(g => g is { Id: 366, Delay: 0, Height: 124 }));
    }

    [TestMethod]
    [DataRow(0, 361, 9, 16)]
    [DataRow(1, 363, 18, 25)]
    [DataRow(2, 367, 27, 34)]
    public void Ice_SuccessfulHit_FreezesAndShowsHitGraphic(int type, int graphic, int ticks, int immunity)
    {
        var scenario = new Scenario();
        scenario.Victim.Freeze(ticks, immunity).Returns(true);

        new AncientIceCombatSpell(type, Definition("ice", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 10);

        scenario.Victim.Received(1).Freeze(ticks, immunity);
        Assert.IsTrue(scenario.VictimGraphics.Any(g => g is { Id: var id, Delay: 61 } && id == graphic));
    }

    [TestMethod]
    [DataRow(true, 369, 0)]
    [DataRow(false, 1677, 150)]
    public void IceBarrage_SuccessfulHit_GraphicDependsOnFreezeOutcome(bool frozen, int graphic, int height)
    {
        var scenario = new Scenario();
        scenario.Victim.Freeze(36, 43).Returns(frozen);

        new AncientIceCombatSpell(3, Definition("ice", 3), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 10);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g.Id == graphic && g.Delay == 61 && g.Height == height));
    }

    [TestMethod]
    public void Ice_MissedHit_ShowsSplashAndDoesNotFreeze()
    {
        var scenario = new Scenario();

        new AncientIceCombatSpell(0, Definition("ice", 0), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 0, succeeded: false);

        scenario.Victim.DidNotReceive().Freeze(Arg.Any<int>(), Arg.Any<int>());
        Assert.IsTrue(scenario.VictimGraphics.Any(g => g is { Id: 85, Delay: 0, Height: 150 }));
    }

    // ---------- Shadow ----------

    [TestMethod]
    [DataRow(0, 378)]
    [DataRow(2, 380)]
    public void Shadow_SendsProjectileOnlyForSingleTargetSpells(int type, int projectile)
    {
        var scenario = new Scenario();

        new AncientShadowCombatSpell(type, Definition("shadow", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        CollectionAssert.AreEqual(new[] { projectile }, scenario.ProjectileGraphics);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    public void Shadow_MultiHitSpells_HaveNoProjectile(int type)
    {
        var scenario = new Scenario();

        new AncientShadowCombatSpell(type, Definition("shadow", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.IsEmpty(scenario.ProjectileGraphics);
    }

    [TestMethod]
    [DataRow(0, 379, 0.90)]
    [DataRow(1, 382, 0.90)]
    [DataRow(2, 381, 0.90)]
    [DataRow(3, 383, 0.85)]
    public void Shadow_SuccessfulHit_ShowsGraphicAndDrainsCharacterAttackToPercentage(int type, int graphic, double percent)
    {
        var scenario = new Scenario(victimIsCharacter: true);
        var victim = (ICharacter)scenario.Victim;
        victim.Statistics.LevelForExperience(StatisticsConstants.Attack).Returns(99);
        victim.Statistics.GetSkillLevel(StatisticsConstants.Attack).Returns(99);

        new AncientShadowCombatSpell(type, Definition("shadow", type), scenario.Projectiles).PerformAttack(scenario.Caster, victim);
        scenario.Resolve(0, damage: 10);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g.Id == graphic && g.Delay == 0));
        var lowest = (int)(99 * percent);
        victim.Statistics.Received(1).DamageSkill(StatisticsConstants.Attack, 99 - lowest);
    }

    [TestMethod]
    public void Shadow_SuccessfulHit_DoesNotDrainBelowFloorOrDrainNpcs()
    {
        var character = new Scenario(victimIsCharacter: true);
        var victim = (ICharacter)character.Victim;
        victim.Statistics.LevelForExperience(StatisticsConstants.Attack).Returns(99);
        victim.Statistics.GetSkillLevel(StatisticsConstants.Attack).Returns(80);
        new AncientShadowCombatSpell(0, Definition("shadow", 0), character.Projectiles).PerformAttack(character.Caster, victim);
        character.Resolve(0, damage: 10);
        victim.Statistics.DidNotReceive().DamageSkill(Arg.Any<int>(), Arg.Any<int>());

        var npc = new Scenario();
        new AncientShadowCombatSpell(0, Definition("shadow", 0), npc.Projectiles).PerformAttack(npc.Caster, npc.Victim);
        npc.Resolve(0, damage: 10);
        Assert.IsTrue(npc.VictimGraphics.Any(g => g.Id == 379));
    }

    [TestMethod]
    public void Shadow_MissedHit_ShowsSplash()
    {
        var scenario = new Scenario();

        new AncientShadowCombatSpell(0, Definition("shadow", 0), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 0, succeeded: false);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g is { Id: 85, Delay: 0, Height: 150 }));
    }

    // ---------- Smoke ----------

    [TestMethod]
    [DataRow(0, 384)]
    [DataRow(1, 388)]
    [DataRow(2, 386)]
    [DataRow(3, 390)]
    public void Smoke_SendsProjectile(int type, int projectile)
    {
        var scenario = new Scenario();

        new AncientSmokeCombatSpell(type, Definition("smoke", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        CollectionAssert.AreEqual(new[] { projectile }, scenario.ProjectileGraphics);
    }

    [TestMethod]
    [DataRow(0, 385)]
    [DataRow(1, 389)]
    [DataRow(2, 387)]
    [DataRow(3, 391)]
    public void Smoke_SuccessfulHit_ShowsGraphicAtHeight(int type, int graphic)
    {
        var scenario = new Scenario();

        new AncientSmokeCombatSpell(type, Definition("smoke", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 10);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g is { Delay: 0, Height: 124 } && g.Id == graphic));
    }

    [TestMethod]
    public void Smoke_MissedHit_ShowsSplash()
    {
        var scenario = new Scenario();

        new AncientSmokeCombatSpell(0, Definition("smoke", 0), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 0, succeeded: false);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g is { Id: 85, Delay: 0, Height: 150 }));
    }

    [TestMethod]
    [DataRow(0, (short)20)]
    [DataRow(1, (short)20)]
    [DataRow(2, (short)48)]
    [DataRow(3, (short)40)]
    public void Smoke_PoisonsVictimWithSpellDamageAboveRollThreshold(int type, short poison)
    {
        // Poison is rolled once per attacked creature (>0.65); run until it lands to avoid seeding the shared RNG.
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var scenario = new Scenario();
            new AncientSmokeCombatSpell(type, Definition("smoke", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
            if (scenario.Victim.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(ICreature.Poison)))
            {
                scenario.Victim.Received(1).Poison(poison);
                return;
            }
        }

        Assert.Fail("Poison never applied across 200 casts.");
    }

    // ---------- Blood ----------

    [TestMethod]
    public void Blood_Rush_SendsProjectile374_OthersNone()
    {
        var rush = new Scenario();
        new AncientBloodCombatSpell(2, Definition("blood", 2), rush.Projectiles).PerformAttack(rush.Caster, rush.Victim);
        CollectionAssert.AreEqual(new[] { 374 }, rush.ProjectileGraphics);

        foreach (var type in new[] { 0, 1, 3 })
        {
            var other = new Scenario();
            new AncientBloodCombatSpell(type, Definition("blood", type), other.Projectiles).PerformAttack(other.Caster, other.Victim);
            Assert.IsEmpty(other.ProjectileGraphics);
        }
    }

    [TestMethod]
    [DataRow(0, 373)]
    [DataRow(1, 376)]
    [DataRow(2, 375)]
    [DataRow(3, 377)]
    public void Blood_ShowsHitGraphicImmediatelyWithDelay(int type, int graphic)
    {
        var scenario = new Scenario();

        new AncientBloodCombatSpell(type, Definition("blood", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g.Id == graphic && g.Delay == 61 && g.Height == 0));
    }

    [TestMethod]
    public void Blood_NegativeDamage_ShowsSplashInsteadOfHitGraphic()
    {
        var scenario = new Scenario { MagicDamage = _ => -1 };

        new AncientBloodCombatSpell(0, Definition("blood", 0), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g is { Id: 85, Delay: 61, Height: 150 }));
        Assert.IsFalse(scenario.VictimGraphics.Any(g => g.Id == 373));
    }

    [TestMethod]
    [DataRow(0, 9, 16)]
    [DataRow(1, 18, 25)]
    [DataRow(2, 27, 34)]
    public void Blood_SuccessfulHit_FreezesAndHealsCasterByQuarterOfDamage(int type, int ticks, int immunity)
    {
        var scenario = new Scenario();

        new AncientBloodCombatSpell(type, Definition("blood", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 41);

        scenario.Victim.Received(1).Freeze(ticks, immunity);
        scenario.Caster.Statistics.Received(1).HealLifePoints(10);
    }

    [TestMethod]
    [DataRow(true, 1)]
    [DataRow(false, 0)]
    public void BloodBarrage_FrozenVictimShowsExtraGraphic(bool frozen, int expectedCount)
    {
        var scenario = new Scenario();
        scenario.Victim.Freeze(36, 43).Returns(frozen);

        new AncientBloodCombatSpell(3, Definition("blood", 3), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 10);

        Assert.AreEqual(expectedCount, scenario.VictimGraphics.Count(g => g.Id == 378 && g.Delay == 61));
    }

    [TestMethod]
    public void Blood_MissedHit_DoesNotFreezeHealOrSplashAgain()
    {
        var scenario = new Scenario();

        new AncientBloodCombatSpell(0, Definition("blood", 0), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 0, succeeded: false);

        scenario.Victim.DidNotReceive().Freeze(Arg.Any<int>(), Arg.Any<int>());
        scenario.Caster.Statistics.DidNotReceive().HealLifePoints(Arg.Any<int>());
        Assert.IsFalse(scenario.VictimGraphics.Any(g => g.Id == 85));
    }

    // ---------- Miasmic ----------

    [TestMethod]
    [DataRow(0, 1846, 1846)]
    [DataRow(1, 1848, null)]
    [DataRow(2, 1850, 1852)]
    [DataRow(3, 1853, null)]
    public void Miasmic_ShowsCasterGraphicAndOptionalProjectile(int type, int casterGraphic, int? projectile)
    {
        var scenario = new Scenario();

        new AncientMiasmicCombatSpell(type, Definition("miasmic", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);

        Assert.IsTrue(scenario.CasterGraphics.Any(g => g.Id == casterGraphic && g.Delay == 0 && g.Height == 0));
        CollectionAssert.AreEqual(projectile is { } p ? new[] { p } : Array.Empty<int>(), scenario.ProjectileGraphics);
    }

    [TestMethod]
    [DataRow(0, 1847, 12000 / 600)]
    [DataRow(1, 1849, 2 * 12000 / 600)]
    [DataRow(2, 1851, 3 * 12000 / 600)]
    [DataRow(3, 1854, 4 * 12000 / 600)]
    public void Miasmic_SuccessfulHitOnCharacter_ShowsGraphicAndAppliesSlowAndImmunity(int type, int graphic, int ticks)
    {
        var scenario = new Scenario(victimIsCharacter: true);

        new AncientMiasmicCombatSpell(type, Definition("miasmic", type), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 10);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g.Id == graphic && g.Delay == 0));
        scenario.Victim.Received(1).AddState(Arg.Is<MiasmicSlowState>(s => s.TicksLeft == ticks));
        scenario.Victim.Received(1).AddState(Arg.Is<MiasmicSlowImmunityState>(s => s.TicksLeft == ticks));
    }

    [TestMethod]
    public void Miasmic_SuccessfulHitOnNpc_ShowsGraphicWithoutStates()
    {
        var scenario = new Scenario();

        new AncientMiasmicCombatSpell(0, Definition("miasmic", 0), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 10);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g.Id == 1847));
        scenario.Victim.DidNotReceive().AddState(Arg.Any<IState>());
    }

    [TestMethod]
    public void Miasmic_MissedHit_ShowsSplash()
    {
        var scenario = new Scenario();

        new AncientMiasmicCombatSpell(0, Definition("miasmic", 0), scenario.Projectiles).PerformAttack(scenario.Caster, scenario.Victim);
        scenario.Resolve(0, damage: 0, succeeded: false);

        Assert.IsTrue(scenario.VictimGraphics.Any(g => g is { Id: 85, Delay: 0, Height: 150 }));
    }

    [TestMethod]
    public void Miasmic_RequiresEquippedZurielsStaff()
    {
        var scenario = new Scenario();
        var spell = new AncientMiasmicCombatSpell(0, Definition("miasmic", 0), scenario.Projectiles);
        scenario.Caster.Magic.CheckMagicLevel(Arg.Any<int>()).Returns(true);
        scenario.Caster.Magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(true);

        scenario.Caster.Equipment.Items.GetById(Arg.Any<int>()).Returns(_ => null);

        Assert.IsFalse(spell.CheckRequirements(scenario.Caster));
        scenario.Caster.Received(1).SendChatMessage("You must have Zuriel's staff equipped to cast this spell.");

        var staff = Substitute.For<Hagalaz.Game.Abstractions.Model.Items.IItem>();
        scenario.Caster.Equipment.Items.GetById(13869).Returns(staff);
        Assert.IsTrue(spell.CheckRequirements(scenario.Caster));
    }

    /// <summary>
    ///     The persisted definition of a spell as the database seeds it; values are the ones the spell classes used to hard-code.
    /// </summary>
    private static CombatSpellDto Definition(string family, int type)
    {
        var (firstButton, runes, counts, levels, configs, experience, damage) = Families[family];
        return new CombatSpellDto
        {
            ButtonId = firstButton + Array.IndexOf(ButtonTypeOrder, type),
            SpellBook = MagicBook.AncientBook,
            RequiredLevel = levels[type],
            RequiredRunes = runes[type],
            RequiredRunesCounts = counts[type],
            BaseExperience = experience[type],
            BaseDamage = damage[type],
            AutoCastConfig = configs[type],
            CastAnimationId = -1,
            CastGraphicId = -1,
            EndGraphicId = -1,
            ProjectileId = -1
        };
    }

    private static readonly int[] ButtonTypeOrder = [0, 2, 1, 3];

    private static readonly Dictionary<string, (int FirstButton, RuneType[][] Runes, int[][] Counts, int[] Levels, int[] Configs, double[] Experience, int[] Damage)> Families = new()
    {
        ["ice"] = (20, [[RuneType.Chaos, RuneType.Death, RuneType.Water], [RuneType.Chaos, RuneType.Death, RuneType.Water], [RuneType.Death, RuneType.Blood, RuneType.Water], [RuneType.Death, RuneType.Blood, RuneType.Water]],
            [[2, 2, 2], [4, 2, 4], [2, 2, 3], [4, 2, 6]], [58, 70, 82, 94], [69, 77, 85, 93], [34, 40, 46, 52], [180, 220, 260, 300]),
        ["blood"] = (24, [[RuneType.Chaos, RuneType.Death, RuneType.Blood], [RuneType.Chaos, RuneType.Death, RuneType.Blood], [RuneType.Death, RuneType.Blood], [RuneType.Death, RuneType.Blood, RuneType.Soul]],
            [[2, 2, 1], [4, 2, 2], [2, 4], [4, 2, 1]], [56, 68, 80, 92], [67, 75, 83, 91], [33, 39, 45, 51], [150, 210, 250, 290]),
        ["smoke"] = (28, [[RuneType.Chaos, RuneType.Death, RuneType.Fire, RuneType.Air], [RuneType.Chaos, RuneType.Death, RuneType.Fire, RuneType.Air], [RuneType.Death, RuneType.Blood, RuneType.Fire, RuneType.Air], [RuneType.Death, RuneType.Blood, RuneType.Fire, RuneType.Air]],
            [[2, 2, 1, 1], [4, 2, 2, 2], [2, 2, 2, 2], [4, 2, 4, 4]], [50, 62, 74, 86], [63, 71, 79, 87], [30, 36, 42, 48], [130, 170, 264, 270]),
        ["shadow"] = (32, [[RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul], [RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul]],
            [[2, 2, 1, 1], [4, 2, 1, 2], [2, 2, 2, 2], [4, 2, 4, 3]], [52, 64, 76, 88], [65, 73, 81, 89], [31, 37, 43, 49], [140, 180, 240, 280]),
        ["miasmic"] = (36, [[RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Blood, RuneType.Earth, RuneType.Soul], [RuneType.Blood, RuneType.Earth, RuneType.Soul]],
            [[2, 1, 1], [4, 2, 2], [2, 3, 3], [4, 4, 4]], [61, 73, 85, 97], [95, 97, 99, 101], [36, 42, 48, 54], [180, 240, 280, 352])
    };

    private static ICombatSpell Create(string family, int type, Scenario scenario) => family switch
    {
        "ice" => new AncientIceCombatSpell(type, Definition("ice", type), scenario.Projectiles),
        "shadow" => new AncientShadowCombatSpell(type, Definition("shadow", type), scenario.Projectiles),
        "smoke" => new AncientSmokeCombatSpell(type, Definition("smoke", type), scenario.Projectiles),
        "blood" => new AncientBloodCombatSpell(type, Definition("blood", type), scenario.Projectiles),
        "miasmic" => new AncientMiasmicCombatSpell(type, Definition("miasmic", type), scenario.Projectiles),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    private sealed class Scenario
    {
        private readonly List<Action<AttackResult>> _handlers = [];
        private readonly List<ICreature> _visible = [];
        private int _nextSlot = 1;

        public Scenario(bool victimIsCharacter = false)
        {
            Caster = Substitute.For<ICharacter>();
            CharacterCombat = Substitute.For<ICharacterCombat, ICreatureCombat>();
            Caster.Combat.Returns((ICreatureCombat)CharacterCombat);
            var casterLocation = Location.Create(10, 10);
            Caster.Location.Returns(casterLocation);
            var casterArea = CreateArea();
            Caster.Area.Returns(casterArea);
            var casterStatistics = Substitute.For<ICharacterStatistics>();
            Caster.Statistics.Returns(casterStatistics);
            Caster.Viewport.VisibleCreatures.Returns(_visible);
            Caster.When(c => c.QueueAnimation(Arg.Any<IAnimation>())).Do(c => CasterAnimations.Add(c.Arg<IAnimation>().Id));
            Caster.When(c => c.QueueGraphic(Arg.Any<IGraphic>())).Do(c => CasterGraphics.Add(c.Arg<IGraphic>()));

            Victim = victimIsCharacter ? Substitute.For<ICharacter>() : Substitute.For<ICreature>();
            ConfigureCreature(Victim, 12, 10);
            if (Victim is ICharacter victimCharacter)
            {
                var victimStatistics = Substitute.For<ICharacterStatistics>();
                victimCharacter.Statistics.Returns(victimStatistics);
            }

            _visible.Add(Victim);

            var handle = Substitute.For<IRsTaskHandle<AttackResult>>();
            handle.When(h => h.RegisterResultHandler(Arg.Any<Action<AttackResult>>()))
                .Do(c => _handlers.Add(c.Arg<Action<AttackResult>>()));
            ((ICreatureCombat)CharacterCombat).PerformAttack(Arg.Do<AttackParams>(Attacks.Add)).Returns(handle);
            CharacterCombat.GetMagicMaxHit(Arg.Any<ICreature>(), Arg.Any<int>()).Returns(c => c.ArgAt<int>(1));
            CharacterCombat.GetMagicDamage(Arg.Any<ICreature>(), Arg.Any<int>()).Returns(c => MagicDamage(c.ArgAt<int>(1)));

            var projectileId = Substitute.For<IProjectileId>();
            ProjectileId = projectileId;
            Projectiles = Substitute.For<IProjectileBuilder>();
            Projectiles.Create().Returns(projectileId);
        }

        public ICharacter Caster { get; }
        public ICharacterCombat CharacterCombat { get; }
        public ICreature Victim { get; }
        public IProjectileBuilder Projectiles { get; }
        public IProjectileId ProjectileId { get; }
        public List<AttackParams> Attacks { get; } = [];
        public List<int> CasterAnimations { get; } = [];
        public List<IGraphic> CasterGraphics { get; } = [];
        public List<IGraphic> VictimGraphics { get; } = [];
        public Func<int, int> MagicDamage { get; set; } = maxDamage => maxDamage;

        public bool MultiCombat
        {
            set
            {
                Caster.Area.MultiCombat.Returns(value);
                Victim.Area.MultiCombat.Returns(value);
            }
        }

        public int[] ProjectileGraphics => ProjectileId.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IProjectileId.WithGraphicId))
            .Select(c => (int)c.GetArguments()[0]!)
            .ToArray();

        public ICreature AddBystander(int victimX, int x, int y)
        {
            _ = victimX;
            var creature = Substitute.For<ICreature>();
            ConfigureCreature(creature, x, y);
            _visible.Add(creature);
            return creature;
        }

        /// <summary>Completes the Nth queued attack the way the combat task would.</summary>
        public void Resolve(int index, int damage, bool succeeded = true) =>
            _handlers[index](new AttackResult
            {
                Damage = (succeeded, damage),
                DamageLifePoints = (succeeded, damage)
            });

        private void ConfigureCreature(ICreature creature, int x, int y)
        {
            var location = Location.Create(x, y);
            creature.Location.Returns(location);
            var handle = new EntityHandle<ICreature>(_nextSlot++, 1);
            creature.Handle.Returns(handle);
            var area = CreateArea();
            creature.Area.Returns(area);
            creature.Combat.CanBeAttackedBy(Arg.Any<ICreature>()).Returns(true);
            creature.When(c => c.QueueGraphic(Arg.Any<IGraphic>())).Do(c =>
            {
                if (ReferenceEquals(creature, Victim))
                {
                    VictimGraphics.Add(c.Arg<IGraphic>());
                }
            });
        }

        private static IArea CreateArea()
        {
            var area = Substitute.For<IArea>();
            area.MultiCombat.Returns(true);
            area.IsPvP.Returns(true);
            return area;
        }
    }
}
