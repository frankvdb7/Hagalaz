using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Common.Tasks
{
    /// <summary>
    /// 
    /// </summary>
    public class NpcSpawnPointReachTask : RsTask
    {
        private readonly INpc _npc;
        private readonly bool _force;

        public NpcSpawnPointReachTask(INpc npc, bool force)
        {
            _npc = npc;
            _force = force;
            ExecuteDelay = 0;
            ExecuteHandler = ExecuteAction;
        }

        private void ExecuteAction()
        {
            if (!_force && (_npc.Movement.Locked || _npc.Combat.IsInCombat()))
            {
                return;
            }
            _npc.Movement.ClearQueue();
            _npc.Movement.MovementType = MovementType.Walk;
            var action = new LocationReachTask(_npc, _npc.Bounds.DefaultLocation, (success) =>
            {
                if (success)
                    _npc.ResetFacing();
            });
            _npc.QueueTask(action);
        }
    }
}