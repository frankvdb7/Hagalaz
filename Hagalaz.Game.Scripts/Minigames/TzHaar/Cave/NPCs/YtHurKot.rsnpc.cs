using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.NPCs
{
    [NpcScriptMetaData([2746])]
    public class YtHurKot : NpcScriptBase
    {
        /// <summary>
        ///     The jad.
        /// </summary>
        private INpc? _jad;

        /// <summary>
        ///     The heal tick.
        /// </summary>
        private int _healTick;
        
        private INpcService _npcRegistration;

        /// <summary>
        /// 
        /// </summary>
        private IMapRegionService _regionManager;

        /// <summary>
        ///     Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override bool CanSetTarget(ICreature target)
        {
            if (target.Location.Dimension != Owner.Location.Dimension)
            {
                return false;
            }

            if (!target.Area.Script.CanBeAttacked(target, Owner))
            {
                return false;
            }

            if (!Owner.Area.Script.CanAttack(Owner, target))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Tick's npc.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (_jad == null || _jad.IsDestroyed)
            {
                Owner.QueueTask(() => _npcRegistration.RegisterAsync(Owner));
                return;
            }

            if (Owner.Combat.IsDead)
            {
                return;
            }

            if (!Owner.Combat.IsInCombat() && Owner.WithinRange(_jad, 1))
            {
                HealJad();
            }

            _healTick++;
        }

        /// <summary>
        ///     Heals the jad.
        /// </summary>
        private void HealJad()
        {
            if (_healTick < 5)
            {
                return;
            }

            Owner.FaceCreature(_jad);
            Owner.QueueGraphic(Graphic.Create(444));
            _jad.Statistics.HealLifePoints(RandomStatic.Generator.Next(0, 100));
            _healTick = 0;
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
            _npcRegistration = Owner.ServiceProvider.GetRequiredService<INpcService>();
            _regionManager =  Owner.ServiceProvider.GetRequiredService<IMapRegionService>();
            var active = _regionManager.FindRegionsByDimension(Owner.Location.Dimension);
            foreach (var region in active)
            {
                foreach (var npc in region.FindAllNpcs())
                {
                    if (npc.Appearance.CompositeID == 2745)
                    {
                        _jad = npc;
                        return;
                    }
                }
            }
        }

        /// <summary>
        ///     Get's called when npc is interrupted.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="source">
        ///     Object which performed the interruption,
        ///     this parameter can be null , but it is not encouraged to do so.
        ///     Best use would be to set the invoker class instance as source.
        /// </param>
        public override void OnInterrupt(object source) => _healTick = 0;
    }
}