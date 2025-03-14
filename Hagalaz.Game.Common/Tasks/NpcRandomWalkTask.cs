using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Common.Tasks
{
    /// <summary>
    /// 
    /// </summary>
    public class NpcRandomWalkTask : RsTickTask
    {
        private readonly INpc _npc;
        private readonly IPathFinder _pathFinder;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="npc"></param>
        public NpcRandomWalkTask(INpc npc)
        {
            _npc = npc;
            _pathFinder = npc.ServiceProvider.GetRequiredService<IPathFinderProvider>().Simple;
            TickActionMethod = PerformTickImpl;
        }

        /// <summary>
        /// Implementation of superclass method callback.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (_npc.Movement.Locked || _npc.Movement.Moving || _npc.Combat.IsInCombat())
            {
                return;
            }
            if (RandomStatic.Generator.NextDouble() * 1000.0 > 10.0)
            {
                return;
            }
            int rndX = (int)Math.Round(RandomStatic.Generator.NextDouble() * 10.0 - 5.0);
            int rndY = (int)Math.Round(RandomStatic.Generator.NextDouble() * 10.0 - 5.0);
            if (rndX == 0 && rndY == 0)
            {
                return;
            }

            int targetX = _npc.Location.X + rndX;
            int targetY = _npc.Location.Y + rndY;
            if (targetX < _npc.Bounds.MinimumLocation.X)
                targetX = _npc.Bounds.MinimumLocation.X;
            if (targetX > _npc.Bounds.MaximumLocation.X)
                targetX = _npc.Bounds.MaximumLocation.X;
            if (targetY < _npc.Bounds.MinimumLocation.Y)
                targetY = _npc.Bounds.MinimumLocation.Y;
            if (targetY > _npc.Bounds.MaximumLocation.Y)
                targetY = _npc.Bounds.MaximumLocation.Y;
            var target = Location.Create(targetX, targetY, _npc.Location.Z);
            var current = Location.Create(_npc.Location.X, _npc.Location.Y, _npc.Location.Z);
            if (current.Equals(target))
                return;
            var path = _pathFinder.Find(_npc, target, true);
            if (!path.Successful)
                return;
            _npc.Movement.ClearQueue();
            _npc.Movement.MovementType = MovementType.Walk;
            foreach (var loc in path)
            {
                var location = Location.Create(loc.X, loc.Y, _npc.Location.Z, _npc.Location.Dimension);
                if (!_npc.Bounds.InBounds(location))
                    break;
                _npc.Movement.AddToQueue(location);
            }
        }
    }
}