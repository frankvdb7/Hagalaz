using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Godwars;

/// <summary>
///     Shared scaffolding for NPC attack tests: an owner at (10,10), a target at (13,11), recorded projectile builder calls
///     and captured attacks. Range damage returns 7/9; magic damage returns base+1/base+2.
/// </summary>
internal sealed class NpcAttackWorld
{
    public NpcAttackWorld()
    {
        var ownerLocation = Location.Create(10, 10);
        var targetLocation = Location.Create(13, 11);

        Combat = Substitute.For<ICreatureCombat, INpcCombat>();
        var npcCombat = (INpcCombat)Combat;
        npcCombat.GetRangeDamage(Arg.Any<ICreature>()).Returns(7);
        npcCombat.GetRangeMaxHit(Arg.Any<ICreature>()).Returns(9);
        npcCombat.GetMagicDamage(Arg.Any<ICreature>(), Arg.Any<int>()).Returns(c => c.ArgAt<int>(1) + 1);
        npcCombat.GetMagicMaxHit(Arg.Any<ICreature>(), Arg.Any<int>()).Returns(c => c.ArgAt<int>(1) + 2);
        Combat.PerformAttack(Arg.Do<AttackParams>(Attacks.Add)).Returns(Substitute.For<IRsTaskHandle<AttackResult>>());

        Owner = Substitute.For<INpc>();
        Owner.Combat.Returns(Combat);
        Owner.Location.Returns(ownerLocation);
        Owner.Definition.AttackAnimation.Returns(-1);
        Owner.Definition.AttackGraphic.Returns(-1);

        Target = Substitute.For<ICreature>();
        Target.Location.Returns(targetLocation);
        var handle = new EntityHandle<ICreature>(5, 1);
        Target.Handle.Returns(handle);
        TargetHandle = handle;

        Recorder = Substitute.For(
            [typeof(IProjectileId), typeof(IProjectileFrom), typeof(IProjectileTo), typeof(IProjectileDuration), typeof(IProjectileOptional)],
            []);
        var optional = (IProjectileOptional)Recorder;
        ((IProjectileId)Recorder).WithGraphicId(Arg.Any<int>()).Returns((IProjectileFrom)Recorder);
        ((IProjectileFrom)Recorder).FromCreature(Arg.Any<ICreature>()).Returns((IProjectileTo)Recorder);
        ((IProjectileTo)Recorder).ToCreature(Arg.Any<ICreature>()).Returns((IProjectileDuration)Recorder);
        ((IProjectileDuration)Recorder).WithDuration(Arg.Any<int>()).Returns(optional);
        optional.WithDelay(Arg.Any<int>()).Returns(optional);
        optional.WithSlope(Arg.Any<int>()).Returns(optional);
        optional.WithAngle(Arg.Any<int>()).Returns(optional);
        optional.WithFromHeight(Arg.Any<int>()).Returns(optional);
        optional.WithToHeight(Arg.Any<int>()).Returns(optional);
        Projectiles = Substitute.For<IProjectileBuilder>();
        Projectiles.Create().Returns((IProjectileId)Recorder);
    }

    public ICreatureCombat Combat { get; }
    public INpc Owner { get; }
    public ICreature Target { get; }
    public EntityHandle<ICreature> TargetHandle { get; }
    public IProjectileBuilder Projectiles { get; }
    public object Recorder { get; }
    public INpcService NpcService { get; } = Substitute.For<INpcService>();
    public ISimplePathFinder PathFinder { get; } = Substitute.For<ISimplePathFinder>();
    public IWidgetScriptActivator WidgetActivator { get; } = Substitute.For<IWidgetScriptActivator>();
    public List<AttackParams> Attacks { get; } = [];

    public IReadOnlyList<(string Method, object?[] Args)> ProjectileCalls =>
        Recorder.ReceivedCalls().Select(c => (c.GetMethodInfo().Name, c.GetArguments())).ToList();

    public ICharacter CreateCharacterAttacker(AttackStyle style)
    {
        var attacker = Substitute.For<ICharacter>();
        attacker.Combat.GetAttackStyle().Returns(style);
        return attacker;
    }

    public void AssertProjectile(int graphic, int duration, int delay, int fromHeight = 35, int toHeight = 35, int angle = 180)
    {
        var calls = ProjectileCalls.ToDictionary(c => c.Method, c => c.Args);
        Assert.AreEqual(graphic, calls[nameof(IProjectileId.WithGraphicId)][0]);
        Assert.AreEqual(duration, calls[nameof(IProjectileDuration.WithDuration)][0]);
        Assert.AreEqual(delay, calls[nameof(IProjectileOptional.WithDelay)][0]);
        Assert.AreEqual(angle, calls[nameof(IProjectileOptional.WithAngle)][0]);
        Assert.AreEqual(fromHeight, calls[nameof(IProjectileOptional.WithFromHeight)][0]);
        Assert.AreEqual(toHeight, calls[nameof(IProjectileOptional.WithToHeight)][0]);
        Assert.IsTrue(calls.ContainsKey(nameof(IProjectileBuild.Send)));
    }

    public void AssertRangedAttack(int delay, int damage, int maxDamage)
    {
        var attack = Attacks.Single();
        Assert.AreEqual(TargetHandle, attack.Target);
        Assert.AreEqual(DamageType.StandardRange, attack.DamageType);
        Assert.AreEqual(delay, attack.Delay);
        Assert.AreEqual(damage, attack.Damage);
        Assert.AreEqual(maxDamage, attack.MaxDamage);
    }

    public void AssertMagicAttack(int delay, int baseDamage)
    {
        var attack = Attacks.Single();
        Assert.AreEqual(TargetHandle, attack.Target);
        Assert.AreEqual(DamageType.StandardMagic, attack.DamageType);
        Assert.AreEqual(delay, attack.Delay);
        Assert.AreEqual(baseDamage + 1, attack.Damage);
        Assert.AreEqual(baseDamage + 2, attack.MaxDamage);
    }
}
