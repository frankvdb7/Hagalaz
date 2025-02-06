namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zaros
{
    /// <summary>
    /// </summary>
    /// <seealso cref="NpcScriptBase" />
    // [NpcScriptMetaData([13447])]
    // public class Nex : NpcScriptBase
    // {
    //     private readonly IAudioBuilder _soundBuilder;
    //
    //     private static readonly ILocation[] NoEscapeTeleports =
    //     [
    //         Location.Create(2924, 5213, 0), // north
    //         Location.Create(2934, 5202, 0), // east
    //         Location.Create(2924, 5192, 0), // south
    //         Location.Create(2913, 5202, 0) // west
    //     ];
    //
    //     private static readonly ILocation Center = Location.Create(2924, 5202, 0);
    //
    //     /// <summary>
    //     ///     The controller
    //     /// </summary>
    //     public CombatPhaseController<AttackRotation> Phases;
    //
    //     /// <summary>
    //     ///     The initial phase
    //     /// </summary>
    //     private CombatPhase<AttackRotation> _initialPhase;
    //
    //     /// <summary>
    //     ///     The smoke phase
    //     /// </summary>
    //     private CombatPhase<AttackRotation> _smokePhase;
    //
    //     /// <summary>
    //     ///     The virus attack
    //     /// </summary>
    //     private AttackRotation _virusAttack;
    //
    //     /// <summary>
    //     ///     The drag attack
    //     /// </summary>
    //     private AttackRotation _dragAttack;
    //
    //     /// <summary>
    //     ///     The no escape attack
    //     /// </summary>
    //     private AttackRotation _noEscapeAttack;
    //
    //     /// <summary>
    //     ///     The magic attack
    //     /// </summary>
    //     private AttackRotation _magicAttack;
    //
    //     /// <summary>
    //     ///     The melee attack
    //     /// </summary>
    //     private AttackRotation _meleeAttack;
    //
    //     /// <summary>
    //     ///     The path finder
    //     /// </summary>
    //     private IPathFinder _pathFinder;
    //
    //     /// <summary>
    //     ///     The NPC definitionRepository
    //     /// </summary>
    //     private INpcService _npcDefinitionRepository;
    //
    //     public Nex(IAudioBuilder soundBuilder)
    //     {
    //         _soundBuilder = soundBuilder;
    //     }
    //
    //     /// <summary>
    //     ///     Perform's attack on specific target.
    //     /// </summary>
    //     /// <param name="target">The target.</param>
    //     public override void PerformAttack(ICreature target) => Phases.ActivePhase?.ActiveRotation?.Perform();
    //
    //     /// <summary>
    //     ///     Happens when the attack has been performed on the target.
    //     ///     By default this method will do nothing.
    //     /// </summary>
    //     /// <param name="target">The target.</param>
    //     public override void OnAttackPerformed(ICreature target) => Phases.ActivePhase?.SelectNewRotation(CombatSelectors.ProbabilitySelector<AttackRotation>());
    //
    //     /// <summary>
    //     ///     Get's if this npc can be attacked by the specified character ('attacker').
    //     ///     By default , this method does check if this npc is attackable.
    //     ///     This method also checks if the attacker is a character, wether or not it
    //     ///     has the required slayer level.
    //     /// </summary>
    //     /// <param name="attacker">Creature which is attacking this npc.</param>
    //     /// <returns>
    //     ///     If attack can be performed.
    //     /// </returns>
    //     public override bool CanBeAttackedBy(ICreature attacker)
    //     {
    //         if (_initialPhase.Active)
    //         {
    //             return false;
    //         }
    //
    //         if (_noEscapeAttack.Active || _dragAttack.Active)
    //         {
    //             return false;
    //         }
    //
    //         return base.CanBeAttackedBy(attacker);
    //     }
    //
    //     /// <summary>
    //     ///     Get's if this npc can attack the specified character ('target').
    //     ///     By default , this method returns true.
    //     /// </summary>
    //     /// <param name="target">Creature which is being attacked by this npc.</param>
    //     /// <returns>
    //     ///     If attack can be performed.
    //     /// </returns>
    //     public override bool CanAttack(ICreature target)
    //     {
    //         if (_initialPhase.Active)
    //         {
    //             return false;
    //         }
    //
    //         return target.Area == Owner.Area;
    //     }
    //
    //     /// <summary>
    //     ///     Get's attack bonus type of this npc.
    //     ///     By default , this method does return AttackBonus.Crush
    //     /// </summary>
    //     /// <returns>
    //     ///     AttackBonus.
    //     /// </returns>
    //     public override AttackBonus GetAttackBonusType()
    //     {
    //         if (_magicAttack.Active)
    //         {
    //             return AttackBonus.Magic;
    //         }
    //
    //         if (_meleeAttack.Active || _dragAttack.Active || _noEscapeAttack.Active)
    //         {
    //             return AttackBonus.Slash;
    //         }
    //
    //         return base.GetAttackBonusType();
    //     }
    //
    //     /// <summary>
    //     ///     Get's attack style of this npc.
    //     ///     By default , this method does return AttackStyle.Accurate.
    //     /// </summary>
    //     /// <returns>
    //     ///     AttackStyle.
    //     /// </returns>
    //     public override AttackStyle GetAttackStyle()
    //     {
    //         if (_magicAttack.Active)
    //         {
    //             return AttackStyle.MagicNormal;
    //         }
    //
    //         if (_meleeAttack.Active || _dragAttack.Active || _noEscapeAttack.Active)
    //         {
    //             return AttackStyle.MeleeAccurate;
    //         }
    //
    //         return base.GetAttackStyle();
    //     }
    //
    //     /// <summary>
    //     ///     Get's attack distance of this npc.
    //     /// </summary>
    //     /// <returns>
    //     ///     System.Int32.
    //     /// </returns>
    //     public override int GetAttackDistance()
    //     {
    //         if (_magicAttack.Active)
    //         {
    //             return 7;
    //         }
    //
    //         if (_meleeAttack.Active)
    //         {
    //             return 1;
    //         }
    //
    //         return base.GetAttackDistance();
    //     }
    //
    //     /// <summary>
    //     ///     Get's attack speed of this npc.
    //     ///     By default, this method does return Definition.AttackSpeed.
    //     /// </summary>
    //     /// <returns>
    //     ///     System.Int32.
    //     /// </returns>
    //     public override int GetAttackSpeed()
    //     {
    //         if (_initialPhase.Active)
    //         {
    //             return 8;
    //         }
    //
    //         if (_magicAttack.Active)
    //         {
    //             return 6;
    //         }
    //
    //         return base.GetAttackSpeed();
    //     }
    //
    //     /// <summary>
    //     ///     Get's if this npc can aggro attack specific character.
    //     ///     By default this method does check if character is character.
    //     ///     This method does not get called by ticks if npc reaction is not aggresive.
    //     /// </summary>
    //     /// <param name="creature">The creature.</param>
    //     /// <returns>
    //     ///     <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
    //     /// </returns>
    //     public override bool IsAggressiveTowards(ICreature creature)
    //     {
    //         if (creature.IsDestroyed)
    //         {
    //             return false;
    //         }
    //
    //         if (Owner.Area == creature.Area)
    //         {
    //             return true;
    //         }
    //
    //         return false;
    //     }
    //
    //     /// <summary>
    //     ///     Get's called when npc is spawned.
    //     ///     By default, this method does nothing.
    //     /// </summary>
    //     public override void OnSpawn() => Phases.ActivePhase = _initialPhase;
    //
    //     /// <summary>
    //     ///     Get's called when owner is found.
    //     /// </summary>
    //     protected override void Initialize()
    //     {
    //         _pathFinder = Owner.ServiceProvider.GetRequiredService<ISmartPathFinder>();
    //         _npcDefinitionRepository = Owner.ServiceProvider.GetRequiredService<INpcService>();
    //         Phases = new CombatPhaseController<AttackRotation>();
    //
    //         _initialPhase = new CombatPhase<AttackRotation>();
    //         _initialPhase.OnActivated += InitialPhase_OnActivated;
    //
    //         _virusAttack = new AttackRotation(() => 5.0);
    //         _virusAttack.OnPerform += Virus_OnPerform;
    //         _dragAttack = new AttackRotation(() =>
    //         {
    //             if (_dragAttack.Active)
    //             {
    //                 return 0.0;
    //             }
    //
    //             if (!Owner.WithinRange(Owner.Combat.Target, 5))
    //             {
    //                 return 5.0;
    //             }
    //
    //             return 0.0;
    //         });
    //         _dragAttack.OnPerform += Drag_OnPerform;
    //         _noEscapeAttack = new AttackRotation(() =>
    //         {
    //             if (_noEscapeAttack.Active)
    //             {
    //                 return 0.0;
    //             }
    //
    //             return 2.5;
    //         });
    //         _noEscapeAttack.OnPerform += NoEscape_OnPerform;
    //         _magicAttack = new AttackRotation(() =>
    //         {
    //             if (!Owner.WithinRange(Owner.Combat.Target, 7))
    //             {
    //                 return 75.0;
    //             }
    //
    //             return 25.0;
    //         });
    //         _magicAttack.OnPerform += MagicAttack_OnPerform;
    //         _meleeAttack = new AttackRotation(() =>
    //         {
    //             if (Owner.WithinRange(Owner.Combat.Target, 1))
    //             {
    //                 return 75.0;
    //             }
    //
    //             return 25.0;
    //         });
    //         _meleeAttack.OnPerform += MeleeAttack_OnPerform;
    //
    //         _noEscapeAttack.OnActivated += () =>
    //         {
    //             //Program.Logger.LogDebug("Magic Activated");
    //         };
    //         _virusAttack.OnActivated += () =>
    //         {
    //             //Program.Logger.LogDebug("Virus Activated");
    //         };
    //         _dragAttack.OnActivated += () =>
    //         {
    //             //Program.Logger.LogDebug("Drag Activated");
    //         };
    //         _magicAttack.OnActivated += () =>
    //         {
    //             //Program.Logger.LogDebug("Magic Activated");
    //         };
    //         _meleeAttack.OnActivated += () =>
    //         {
    //             //Program.Logger.LogDebug("Melee Activated");
    //         };
    //
    //         _smokePhase = new CombatPhase<AttackRotation>([_virusAttack, _dragAttack, _magicAttack, _meleeAttack]);
    //         _smokePhase.OnActivated += SmokePhase_OnActivated;
    //
    //         Owner.Movement.MovementType = MovementType.Run;
    //     }
    //
    //     /// <summary>
    //     ///     Noes the escape on perform.
    //     /// </summary>
    //     private void NoEscape_OnPerform()
    //     {
    //         Owner.Speak("There is...");
    //         _soundBuilder.Create().AsVoice().WithId(3294).Build().PlayWithinDistance(Owner, 8);
    //
    //         var index = RandomStatic.Generator.Next(0, 4);
    //         var dest = NoEscapeTeleports[index];
    //         var center = Center.Clone();
    //         RsTickTask task = null;
    //         Owner.QueueTask(task = new RsTickTask(() =>
    //         {
    //             if (task.TickCount == 1)
    //             {
    //                 Owner.QueueAnimation(Animation.Create(17411));
    //                 Owner.QueueGraphic(Graphic.Create(1216));
    //             }
    //             else if (task.TickCount == 4)
    //             {
    //                 Owner.Speak("NO ESCAPE!");
    //                 _soundBuilder.Create().AsVoice().WithId(3292).Build().PlayWithinDistance(Owner, 8);
    //
    //                 var characters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
    //                 foreach (var character in characters)
    //                 {
    //                     //int dir = DirectionUtilities.CalculateBasicFaceDirection(character.Location, center);
    //                     //if (index == dir)
    //                     //{
    //                     //    character.QueueAnimation(Animation.Create(10070));
    //                     //    character.RenderNonstandardMovement(character.Location, 2, center, 2, (Direction)dir);
    //                     //    this.owner.Combat.PerformHit(character, DamageType.Regular, HitSplatType.HitSimpleDamage, ((Hagalaz.Game.Model.Npcs.Combat)this.owner.Combat).GetMeleeDamage(character, 700), 700);
    //                     //}
    //                 }
    //
    //                 _noEscapeAttack.Deactivate();
    //                 task.Cancel();
    //             }
    //         }));
    //     }
    //
    //     /// <summary>
    //     ///     Melees the attack on perform.
    //     /// </summary>
    //     private void MeleeAttack_OnPerform()
    //     {
    //         var target = Owner.Combat.Target;
    //         RenderAttack();
    //         Owner.Combat.PerformHit(target, DamageType.StandartMelee, HitSplatType.HitMeleeDamage, ((INpcCombat)Owner.Combat).GetMeleeDamage(target), ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target), 50);
    //     }
    //
    //     /// <summary>
    //     ///     Magics the attack on perform.
    //     /// </summary>
    //     private void MagicAttack_OnPerform()
    //     {
    //         Owner.QueueAnimation(Animation.Create(17413));
    //         //this.owner.QueueGraphic(Graphic.Create(1215, 0, 100));
    //         Owner.QueueTask(new RsTask(() =>
    //         {
    //             var characters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
    //             foreach (var character in characters)
    //             {
    //                 var magicMaxHit = ((INpcCombat) Owner.Combat).GetMagicMaxHit(character, 250);
    //                 var magicDamage = ((INpcCombat) Owner.Combat).GetMagicDamage(character, 250);
    //                 var duration = 0;
    //                 Owner.Combat.PerformDelayedHit(character, DamageType.StandartMagic, HitSplatType.HitMagicDamage, magicDamage, magicMaxHit, ref duration, damage =>
    //                 {
    //                     character.QueueGraphic(Graphic.Create(3373));
    //                     if (damage > 0 && RandomStatic.Generator.Next(0, 5) == 0)
    //                     {
    //                         character.Poison(80);
    //                     }
    //                 });
    //
    //                 var projectile = new Projectile(3371);
    //                 projectile.SetSenderData(Owner, 0, false);
    //                 projectile.SetReceiverData(character, 0);
    //                 projectile.SetFlyingProperties(0, duration, 0, 0, false);
    //                 projectile.Display();
    //             }
    //         }, 2));
    //     }
    //
    //     /// <summary>
    //     ///     Drags the on perform.
    //     /// </summary>
    //     private void Drag_OnPerform()
    //     {
    //         Owner.Speak("Come with me...");
    //         Owner.Movement.Teleport(Teleport.Create(Center.Clone()));
    //         Owner.QueueTask(new RsTask(() =>
    //         {
    //             var t = Owner.Combat.Target as ICharacter;
    //             t.Movement.Teleport(Teleport.Create(Center.Translate(-1, 1, 0)));
    //             t.SendChatMessage("Nex has stunned you and disabled your prayers!");
    //             t.Stun(5);
    //             t.Prayers.DeactivateAllPrayers();
    //         }, 1));
    //         _dragAttack.Deactivate();
    //     }
    //
    //     /// <summary>
    //     ///     Viruses the on perform.
    //     /// </summary>
    //     private void Virus_OnPerform()
    //     {
    //         Owner.Speak("Let the virus flow through you!");
    //         Owner.QueueAnimation(Animation.Create(17414));
    //         Owner.QueueGraphic(Graphic.Create(3375));
    //         _soundBuilder.Create().AsVoice().WithId(3296).Build().PlayWithinDistance(Owner, 8);
    //         Owner.Combat.Target.QueueGraphic(Graphic.Create(388));
    //         if (!Owner.Combat.Target.HasState(StateType.NexVirus))
    //         {
    //             Owner.Combat.Target.AddState(new State(StateType.NexVirus, int.MaxValue));
    //         }
    //     }
    //
    //     /// <summary>
    //     ///     Smokes the phase on activated.
    //     /// </summary>
    //     private void SmokePhase_OnActivated()
    //     {
    //         Owner.Speak("Fill my soul with smoke...");
    //         _soundBuilder.Create().AsVoice().WithId(3310).Build().PlayWithinDistance(Owner, 8);
    //     }
    //
    //     /// <summary>
    //     ///     Initials the phase on activated.
    //     /// </summary>
    //     private void InitialPhase_OnActivated()
    //     {
    //         Owner.Appearance.Transform(13447);
    //         Owner.QueueAnimation(Animation.Create(17412));
    //         Owner.Speak("AT LAST!");
    //         _soundBuilder.Create().AsVoice().WithId(3295).Build().PlayWithinDistance(Owner, 8);
    //         Owner.QueueTask(new RsTask(() =>
    //         {
    //             SpawnMage(13451, _soundBuilder.Create().AsVoice().WithId(3325).Build(), Location.Create(2913, 5215, 0), Animation.Create(17414));
    //         }, 4));
    //         Owner.QueueTask(new RsTask(() =>
    //         {
    //             SpawnMage(13452, _soundBuilder.Create().AsVoice().WithId(3313).Build(), Location.Create(2937, 5215, 0), Animation.Create(17413));
    //         }, 8));
    //         Owner.QueueTask(new RsTask(() =>
    //         {
    //             SpawnMage(13453, _soundBuilder.Create().AsVoice().WithId(3299).Build(), Location.Create(2937, 5191, 0), Animation.Create(17414));
    //         }, 12));
    //         Owner.QueueTask(new RsTask(() =>
    //         {
    //             SpawnMage(13454, _soundBuilder.Create().AsVoice().WithId(3304).Build(), Location.Create(2913, 5191, 0), Animation.Create(17413));
    //         }, 16));
    //         Owner.QueueTask(new RsTask(() =>
    //         {
    //             Phases.ActivePhase = _smokePhase;
    //         }, 20));
    //     }
    //
    //     /// <summary>
    //     ///     Spawns fumus.
    //     /// </summary>
    //     /// <param name="npcID">The NPC identifier.</param>
    //     /// <param name="sound">The sound.</param>
    //     /// <param name="location">The location.</param>
    //     /// <param name="animation">The animation.</param>
    //     private void SpawnMage(short npcID, ISound sound, ILocation location, IAnimation animation)
    //     {
    //         //var npc = new Npc(npcID, location, true);
    //         // TODO
    //         //_npcDefinitionRepository.Register(npc);
    //
    //         // var projectile = new Projectile(2244);
    //         // projectile.SetSenderData(npc, 43, false);
    //         // projectile.SetReceiverData(Owner, 31);
    //         // projectile.SetFlyingProperties(0, 100, 0, 0, false);
    //         // projectile.Display();
    //         //
    //         // Owner.FaceLocation(npc);
    //         // Owner.QueueAnimation(animation);
    //         // Owner.Speak(npc.Definition.DisplayName + "!");
    //         // sound.PlayWithinDistance(Owner, 8);
    //     }
    //
    //     /// <summary>
    //     ///     Tick's npc.
    //     ///     By default, this method does nothing.
    //     /// </summary>
    //     public override void Tick()
    //     {
    //         if (_meleeAttack.Active)
    //         {
    //             if (!Owner.WithinRange(Owner.Combat.Target, 1))
    //             {
    //                 if (RandomStatic.Generator.Next(0, 5) == 0)
    //                 {
    //                     Phases.ActivePhase?.SelectNewRotation(CombatSelectors.ProbabilitySelector<AttackRotation>()); // select new attack if target is to far away
    //                 }
    //             }
    //         }
    //     }
    //
    //     /// <summary>
    //     ///     Contains the route finder the NPC will use.
    //     ///     By default, this method returns the simple route finder when in combat
    //     ///     and the standart route finder for random walking.
    //     /// </summary>
    //     /// <returns></returns>
    //     public override IPathFinder GetPathfinder() => _pathFinder;
    // }
}