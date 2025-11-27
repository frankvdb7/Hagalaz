using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    ///     Action for pickpocketing.
    /// </summary>
    public class StandardPickPocketTask : RsTask
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardPickPocketTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="npc">The NPC.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="maximumCount">The maximum count.</param>
        /// <param name="executeDelay">The executing tick.</param>
        public StandardPickPocketTask(ICharacter performer, INpc npc, PickPocketDefinition definition, int maximumCount, int executeDelay)
        {
            _performer = performer;
            _npc = npc;
            _definition = definition;
            ExecuteDelay = executeDelay;
            ExecuteHandler = OnPerform;
            _maximumLootCount = maximumCount;
            OnInitialized();
        }

        /// <summary>
        ///     Contains the performer.
        /// </summary>
        private readonly ICharacter _performer;

        /// <summary>
        ///     Contains the npc.
        /// </summary>
        private readonly INpc _npc;

        /// <summary>
        ///     Contains the definition.
        /// </summary>
        private readonly PickPocketDefinition _definition;

        /// <summary>
        ///     The maximum loot count.
        /// </summary>
        private int _maximumLootCount;

        /// <summary>
        ///     The extra loot flag.
        /// </summary>
        private int _extraLootFlag;

        /// <summary>
        ///     Called when [initialized].
        /// </summary>
        public virtual void OnInitialized()
        {
            _performer.FaceCreature(_npc);

            var thievingLevel = _performer.Statistics.GetSkillLevel(StatisticsConstants.Thieving);
            var agilityLevel = _performer.Statistics.GetSkillLevel(StatisticsConstants.Agility);
            for (var i = 2; i >= 0; i--)
            {
                var offset = i * 10;
                if (thievingLevel >= _definition.ExtraLootThievingLevel + offset && agilityLevel >= _definition.ExtraLootAgilityLevel + offset)
                {
                    if (i == 2)
                    {
                        _extraLootFlag |= 0x4;
                    }
                    else if (i == 1)
                    {
                        _extraLootFlag |= 0x2;
                    }
                    else if (i == 0)
                    {
                        _extraLootFlag |= 0x1;
                    }

                    break;
                }
            }

            if (_extraLootFlag == 0)
            {
                _performer.QueueAnimation(Animation.Create(Thieving.PickPocketingAnimation));
            }
            else if ((_extraLootFlag & 0x1) != 0)
            {
                _performer.QueueAnimation(Animation.Create(Thieving.DoubleLootAnimation));
                _performer.QueueGraphic(Graphic.Create(Thieving.DoubleLootGraphic));
                _maximumLootCount += 1;
            }
            else if ((_extraLootFlag & 0x2) != 0)
            {
                _performer.QueueAnimation(Animation.Create(Thieving.TripleLootAnimation));
                _performer.QueueGraphic(Graphic.Create(Thieving.TripleLootGraphic));
                _maximumLootCount += 2;
            }
            else if ((_extraLootFlag & 0x4) != 0)
            {
                _performer.QueueAnimation(Animation.Create(Thieving.QuadrupleLootAnimation));
                _performer.QueueGraphic(Graphic.Create(Thieving.QuadrupleLootGraphic));
                _maximumLootCount += 3;
            }

            _performer.SendChatMessage("You attempt to pick the " + _npc.Name.ToLower() + "'s pocket...");
        }

        /// <summary>
        ///     Called when [perform].
        /// </summary>
        public virtual void OnPerform()
        {
            _performer.RemoveState<ThievingNpcState>(); //always remove the state, otherwise the character can not steal again.
            if (_npc.Combat.IsDead)
            {
                _performer.SendChatMessage("Too late; they are dead.");
                return;
            }

            var thievingLevel = _performer.Statistics.GetSkillLevel(StatisticsConstants.Thieving);
            var agilityLevel = (int)(_performer.Statistics.GetSkillLevel(StatisticsConstants.Agility) * 0.25);
            var totalLevel = thievingLevel + agilityLevel + 1;
            var requiredLevel = (int)(_definition.RequiredLevel * (1.05 + 1.0 / thievingLevel));
            if (RandomStatic.Generator.Next(totalLevel) < RandomStatic.Generator.Next(requiredLevel)) // failed
            {
                _performer.SendChatMessage("You failed to pick the " + _npc.Name.ToLower() + "'s pocket.");
                _npc.QueueAnimation(Animation.Create(_npc.Definition.AttackAnimation));
                _npc.Speak(_npc.Name.Equals("Farmer") || _npc.Name.Equals("Master Farmer")
                    ? "Cor blimey mate, what are ye doing in me pockets?"
                    : "What do you think you're doing?");
                _performer.QueueAnimation(Animation.Create(Thieving.StunnedAnimation));
                _performer.QueueGraphic(Graphic.Create(80, 0, 60));
                _performer.Stun(6); // 4 sec

                var hitSplatBuilder = _performer.ServiceProvider.GetRequiredService<IHitSplatBuilder>();
                var hitSplat = hitSplatBuilder.Create()
                    .AddSprite(builder => builder.
                        WithSplatType(HitSplatType.HitSimpleDamage).
                        WithDamage(_definition.FailDamage))
                    .Build();
                _performer.QueueHitSplat(hitSplat);
                _performer.Statistics.DamageLifePoints(_definition.FailDamage);
            }
            else
            {
                if (_extraLootFlag == 0)
                {
                    _performer.SendChatMessage("You successfully pick the " + _npc.Name.ToLower() + "'s pocket.");
                }
                else if ((_extraLootFlag & 0x1) != 0)
                {
                    _performer.SendChatMessage("Your lighting-fast reactions allow you to steal double loot.");
                }
                else if ((_extraLootFlag & 0x2) != 0)
                {
                    _performer.SendChatMessage("Your lighting-fast reactions allow you to steal triple loot.");
                }
                else if ((_extraLootFlag & 0x4) != 0)
                {
                    _performer.SendChatMessage("Your lighting-fast reactions allow you to steal quadruple loot.");
                }

                _performer.Statistics.AddExperience(StatisticsConstants.Thieving, _definition.Experience);

                var lootGenerator = _npc.ServiceProvider.GetRequiredService<ILootGenerator>();
                var lootService = _npc.ServiceProvider.GetRequiredService<ILootService>();
                _npc.QueueTask(async () =>
                {
                    var table = await lootService.FindNpcLootTable(_npc.Definition.PickPocketingLootTableId);
                    if (table != null)
                    {
                        _performer.Inventory.TryAddLoot(_performer,
                            lootGenerator.GenerateLoot<ILootItem>(new CharacterLootParams(table, _performer)
                            {
                                MaxCount = _maximumLootCount
                            }),
                            out _);
                    }
                });
            }

            _performer.ResetFacing();
        }
    }
}