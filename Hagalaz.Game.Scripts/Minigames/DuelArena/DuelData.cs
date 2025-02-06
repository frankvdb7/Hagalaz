using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    /// <summary>
    /// </summary>
    public static class DuelData
    {
        /// <summary>
        ///     Gets the duel lobby location.
        /// </summary>
        /// <returns></returns>
        public static ILocation GetDuelLobbyLocation(IMapRegionService mapRegionService)
        {
            var xSw = 3355;
            var ySw = 3262;
            var xNe = 3386;
            var yNe = 3279;
            var randomX = xNe - xSw;
            var randomY = yNe - ySw;
            ILocation? location = null;
            while (true)
            {
                location = Location.Create(xSw + RandomStatic.Generator.Next(randomX - 1), ySw + RandomStatic.Generator.Next(randomY - 1), 0, 0);
                if (mapRegionService.IsAccessible(location))
                {
                    break;
                }
            }

            return location;
        }

        /// <summary>
        ///     Gets the obstacle arena.
        /// </summary>
        /// <param name="arenaId">The random.</param>
        /// <returns></returns>
        public static ILocation GetObstacleArena(int arenaId)
        {
            short xSw = 3370;
            short ySw = 3244;
            short xNe = 3382;
            short yNe = 3258;
            if (arenaId == 1)
            {
                xSw = 3339;
                ySw = 3225;
                xNe = 3351;
                yNe = 3239;
            }
            else if (arenaId == 2)
            {
                xSw = 3370;
                ySw = 3206;
                xNe = 3382;
                yNe = 3220;
            }

            var randomX = xNe - xSw;
            var randomY = yNe - ySw;
            return Location.Create(xSw + RandomStatic.Generator.Next(randomX - 1), ySw + RandomStatic.Generator.Next(randomY) - 1, 0, 0);
        }

        /// <summary>
        ///     Gets the normal arena.
        /// </summary>
        /// <param name="arenaId">The random.</param>
        /// <returns></returns>
        public static ILocation GetNormalArena(int arenaId)
        {
            short xSw = 3339;
            short ySw = 3244;
            short xNe = 3351;
            short yNe = 3258;
            if (arenaId == 1)
            {
                xSw = 3370;
                ySw = 3225;
                xNe = 3382;
                yNe = 3239;
            }
            else if (arenaId == 2)
            {
                xSw = 3339;
                ySw = 3206;
                xNe = 3351;
                yNe = 3220;
            }

            var randomX = xNe - xSw;
            var randomY = yNe - ySw;
            return Location.Create(xSw + RandomStatic.Generator.Next(randomX - 1), ySw + RandomStatic.Generator.Next(randomY - 1), 0, 0);
        }
    }
}