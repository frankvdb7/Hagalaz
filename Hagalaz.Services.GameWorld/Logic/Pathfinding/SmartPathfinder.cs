using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Logic.Pathfinding
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PathFinderBase" />
    public class SmartPathFinder : PathFinderBase, ISmartPathFinder
    {

        private static readonly Location[] _adjacentDelta =
        [
            Location.Left, Location.Right, Location.Down, Location.Up
        ];

        public SmartPathFinder(IMapRegionService regionService) : base(regionService) { }

        /// <summary>
        /// The queue x
        /// </summary>
        private int[] _queueX = default!;

        /// <summary>
        /// The queue y
        /// </summary>
        private int[] _queueY = default!;

        /// <summary>
        /// The costs
        /// </summary>
        private int[,] _costs = default!;

        /// <summary>
        /// The directions
        /// </summary>
        private DirectionFlag[,] _directions = default!;

        /// <summary>
        /// The write path position
        /// </summary>
        private int _writePathPosition;

        /// <summary>
        /// The current map x
        /// </summary>
        private int _currentMapX;

        /// <summary>
        /// The current map y
        /// </summary>
        private int _currentMapY;

        /// <summary>
        /// Finds the adjacent.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public IPath? FindAdjacent(IEntity entity) => _adjacentDelta
            .Select(delta => Find(entity, entity.Location.Translate(delta.X, delta.Y, delta.Z), false))
            .FirstOrDefault(p => p.Successful);

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            FoundPath = false;
            _writePathPosition = 0;
            _queueX = new int[QueueSize];
            _queueY = new int[QueueSize];
            _costs = new int[GraphSize, GraphSize];
            _directions = new DirectionFlag[GraphSize, GraphSize];

            for (var mapX = 0; mapX < GraphSize; mapX++)
            {
                for (var mapY = 0; mapY < GraphSize; mapY++)
                {
                    _directions[mapX, mapY] = 0;
                    _costs[mapX, mapY] = 99999999;
                }
            }
        }

        /// <summary>
        /// Checks the specified tile.
        /// </summary>
        /// <param name="mapX">The map x.</param>
        /// <param name="mapY">The map y.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="currentCost">The current cost.</param>
        public void Check(int mapX, int mapY, DirectionFlag dir, int currentCost)
        {
            _queueX[_writePathPosition] = mapX;
            _queueY[_writePathPosition] = mapY;
            _directions[mapX, mapY] = dir;
            _costs[mapX, mapY] = currentCost;
            _writePathPosition = _writePathPosition + 1 & 0xfff;
        }

        /// <summary>
        /// Finds a path from the location to the end location.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="to">To.</param>
        /// <param name="targetSizeX">The target size x.</param>
        /// <param name="targetSizeY">The target size y.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="surroundings">The surroundings.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        public override IPath Find(IVector3 from, int selfSize, IVector3 to, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings, bool moveNear)
        {
            Reset();

            var path = new Path();
            var fromMapX = from.GetLocalX(from, GraphSize);
            var fromMapY = from.GetLocalY(from, GraphSize);
            var toMapX = to.GetLocalX(from, GraphSize);
            var toMapY = to.GetLocalY(from, GraphSize);

            var baseLocation = Location.Create(from.GetRegionPartX() - 6 << 3, from.GetRegionPartY() - 6 << 3, from.Z);
            _currentMapX = fromMapX;
            _currentMapY = fromMapY;
            Check(fromMapX, fromMapY, DirectionFlag.None, 0);

            if (selfSize < 2)
            {
                FindSingleTraversal(baseLocation, to, targetSizeX, targetSizeY, rotation, shape, surroundings);
            }
            else if (selfSize == 2)
            {
                FindDoubleTraversal(baseLocation, to, targetSizeX, targetSizeY, rotation, shape, surroundings);
            }
            else
            {
                FindVariableTraversal(baseLocation, to, selfSize, targetSizeX, targetSizeY, rotation, shape, surroundings);
            }

            if (!FoundPath)
            {
                if (moveNear)
                {
                    var fullCost = 1000;
                    var thisCost = 100;
                    for (var x = toMapX - AlternativePathRange; x <= toMapX + AlternativePathRange; x++)
                    {
                        for (var y = toMapY - AlternativePathRange; y <= toMapY + AlternativePathRange; y++)
                        {
                            if (x >= 0 && y >= 0 && x < GraphSize && y < GraphSize && _costs[x, y] < 100)
                            {
                                var diffX = 0;
                                if (x < toMapX)
                                {
                                    diffX = toMapX - x;
                                }
                                else if (x > toMapX + targetSizeX - 1)
                                {
                                    diffX = x - (toMapX + targetSizeX - 1);
                                }

                                var diffY = 0;
                                if (y < toMapY)
                                {
                                    diffY = toMapY - y;
                                }
                                else if (y > toMapY + targetSizeY - 1)
                                {
                                    diffY = y - (toMapY + targetSizeY - 1);
                                }

                                var totalCost = diffX * diffX + diffY * diffY;
                                if (totalCost < fullCost || totalCost == fullCost && _costs[x, y] < thisCost)
                                {
                                    fullCost = totalCost;
                                    thisCost = _costs[x, y];
                                    _currentMapX = x;
                                    _currentMapY = y;
                                }
                            }
                        }
                    }

                    if (fullCost == 1000)
                    {
                        return path;
                    }

                    path.MovedNear = true;
                }
                else
                {
                    return path;
                }
            }

            path.Steps = 0;
            if (_currentMapX == fromMapX && _currentMapY == fromMapY)
            {
                path.Successful = true;
                return path;
            }

            var readPosition = 0;
            _queueX[readPosition] = _currentMapX;
            _queueY[readPosition++] = _currentMapY;
            DirectionFlag previousDirection;
            var currentDirection = previousDirection = _directions[_currentMapX, _currentMapY];
            while (fromMapX != _currentMapX || _currentMapY != fromMapY)
            {
                if (QueueSize <= ++path.Steps)
                {
                    return path;
                }

                if (previousDirection != currentDirection)
                {
                    _queueX[readPosition] = _currentMapX;
                    _queueY[readPosition++] = _currentMapY;
                    previousDirection = currentDirection;
                }

                if ((currentDirection & DirectionFlag.South) != 0)
                    _currentMapY++;
                else if ((currentDirection & DirectionFlag.North) != 0)
                    _currentMapY--;

                if ((currentDirection & DirectionFlag.West) != 0)
                    _currentMapX++;
                else if ((currentDirection & DirectionFlag.East) != 0)
                    _currentMapX--;

                currentDirection = _directions[_currentMapX, _currentMapY];
            }

            while (readPosition-- > 0)
            {
                path.Add(Location.Create(baseLocation.X + _queueX[readPosition], baseLocation.Y + _queueY[readPosition]));
            }

            path.Successful = true;
            return path;
        }

        /// <summary>
        /// Determines whether [has found path] [the specified to map].
        /// </summary>
        /// <param name="to">The dest.</param>
        /// <param name="absX">The abs x.</param>
        /// <param name="absY">The abs y.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="targetSizeX">The target size x.</param>
        /// <param name="targetSizeY">The target size y.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="surroundings">The surroundings.</param>
        /// <param name="z">The z.</param>
        /// <returns>
        ///   <c>true</c> if [has found path] [the specified to map x]; otherwise, <c>false</c>.
        /// </returns>
        private bool HasFoundPath(IVector3 to, int absX, int absY, int selfSize, int targetSizeX, int targetSizeY, int shape, int rotation, int surroundings, int z)
        {
            if (absX == to.X && absY == to.Y)
            {
                return true;
            }

            if (shape != 0)
            {
                if ((shape < 5 || shape == 10) && CanDoorInteract(shape - 1, to.X, to.Y, absX, absY, rotation, selfSize, z))
                {
                    return true;
                }

                if (shape < 10 && CanDecorationInteract(shape - 1, absX, absY, to.X, to.Y, selfSize, rotation, z))
                {
                    return true;
                }
            }

            if (targetSizeX != 0 && targetSizeY != 0 && CanInteract(absX, absY, to.X, to.Y, selfSize, targetSizeX, targetSizeY, surroundings, z))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the single traversal.
        /// </summary>
        /// <param name="base">The base location.</param>
        /// <param name="to">The dest.</param>
        /// <param name="targetSizeX">The target size x.</param>
        /// <param name="targetSizeY">The target size y.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="surroundings">The surroundings.</param>
        private void FindSingleTraversal(IVector3 @base, IVector3 to, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings)
        {
            var readPosition = 0;
            var z = @base.Z;
            while (_writePathPosition != readPosition)
            {
                _currentMapX = _queueX[readPosition];
                _currentMapY = _queueY[readPosition];
                readPosition = readPosition + 1 & 0xfff;

                var absX = @base.X + _currentMapX;
                var absY = @base.Y + _currentMapY;
                if (HasFoundPath(to, absX, absY, 1, targetSizeX, targetSizeY, shape, rotation, surroundings, z))
                {
                    FoundPath = true;
                    break;
                }

                var thisCost = _costs[_currentMapX, _currentMapY] + 1;
                if (_currentMapY > 0 && _directions[_currentMapX, _currentMapY - 1] == 0 && (GetClippingFlag(absX, absY - 1, z) & CollisionFlag.TraversableSouthBlocked) == 0)
                {
                    Check(_currentMapX, _currentMapY - 1, DirectionFlag.South, thisCost);
                }

                if (_currentMapX > 0 && _directions[_currentMapX - 1, _currentMapY] == 0 && (GetClippingFlag(absX - 1, absY, z) & CollisionFlag.TraversableWestBlocked) == 0)
                {
                    Check(_currentMapX - 1, _currentMapY, DirectionFlag.West, thisCost);
                }

                if (_currentMapY < GraphSize - 1 && _directions[_currentMapX, _currentMapY + 1] == 0 && (GetClippingFlag(absX, absY + 1, z) & CollisionFlag.TraversableNorthBlocked) == 0)
                {
                    Check(_currentMapX, _currentMapY + 1, DirectionFlag.North, thisCost);
                }

                if (_currentMapX < GraphSize - 1 && _directions[_currentMapX + 1, _currentMapY] == 0 && (GetClippingFlag(absX + 1, absY, z) & CollisionFlag.TraversableEastBlocked) == 0)
                {
                    Check(_currentMapX + 1, _currentMapY, DirectionFlag.East, thisCost);
                }

                if (_currentMapX > 0 && _currentMapY > 0 && _directions[_currentMapX - 1, _currentMapY - 1] == 0 &&
                    (GetClippingFlag(absX - 1, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX - 1, absY, z) & CollisionFlag.TraversableWestBlocked) == 0 &&
                    (GetClippingFlag(absX, absY - 1, z) & CollisionFlag.TraversableSouthBlocked) == 0)
                {
                    Check(_currentMapX - 1, _currentMapY - 1, DirectionFlag.SouthWest, thisCost);
                }

                if (_currentMapX > 0 && _currentMapY < GraphSize - 1 && _directions[_currentMapX - 1, _currentMapY + 1] == 0 &&
                    (GetClippingFlag(absX - 1, absY + 1, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                    (GetClippingFlag(absX - 1, absY, z) & CollisionFlag.TraversableWestBlocked) == 0 &&
                    (GetClippingFlag(absX, absY + 1, z) & CollisionFlag.TraversableNorthBlocked) == 0)
                {
                    Check(_currentMapX - 1, _currentMapY + 1, DirectionFlag.NorthWest, thisCost);
                }

                if (_currentMapX < GraphSize - 1 && _currentMapY > 0 && _directions[_currentMapX + 1, _currentMapY - 1] == 0 &&
                    (GetClippingFlag(absX + 1, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX + 1, absY, z) & CollisionFlag.TraversableEastBlocked) == 0 &&
                    (GetClippingFlag(absX, absY - 1, z) & CollisionFlag.TraversableSouthBlocked) == 0)
                {
                    Check(_currentMapX + 1, _currentMapY - 1, DirectionFlag.SouthEast, thisCost);
                }

                if (_currentMapX < GraphSize - 1 && _currentMapY < GraphSize - 1 && _directions[_currentMapX + 1, _currentMapY + 1] == 0 &&
                    (GetClippingFlag(absX + 1, absY + 1, z) & CollisionFlag.TraversableNorthEastBlocked) == 0 &&
                    (GetClippingFlag(absX + 1, absY, z) & CollisionFlag.TraversableEastBlocked) == 0 &&
                    (GetClippingFlag(absX, absY + 1, z) & CollisionFlag.TraversableNorthBlocked) == 0)
                {
                    Check(_currentMapX + 1, _currentMapY + 1, DirectionFlag.NorthEast, thisCost);
                }
            }
        }

        /// <summary>
        /// Finds the double traversal.
        /// </summary>
        /// <param name="base">The base location.</param>
        /// <param name="to">To.</param>
        /// <param name="targetSizeX">The target size x.</param>
        /// <param name="targetSizeY">The target size y.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="surroundings">The surroundings.</param>
        private void FindDoubleTraversal(IVector3 @base, IVector3 to, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings)
        {
            var readPosition = 0;
            var z = @base.Z;
            while (_writePathPosition != readPosition)
            {
                _currentMapX = _queueX[readPosition];
                _currentMapY = _queueY[readPosition];
                readPosition = readPosition + 1 & 0xfff;

                var absX = @base.X + _currentMapX;
                var absY = @base.Y + _currentMapY;
                if (HasFoundPath(to, absX, absY, 2, targetSizeX, targetSizeY, shape, rotation, surroundings, z))
                {
                    FoundPath = true;
                    break;
                }

                var thisCost = _costs[_currentMapX, _currentMapY] + 1;
                if (_currentMapY > 0 && _directions[_currentMapX, _currentMapY - 1] == 0 &&
                    (GetClippingFlag(absX, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX + 1, absY - 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0)
                {
                    Check(_currentMapX, _currentMapY - 1, DirectionFlag.South, thisCost);
                }

                if (_currentMapX > 0 && _directions[_currentMapX - 1, _currentMapY] == 0 &&
                    (GetClippingFlag(absX, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX - 1, absY + 1, z) & CollisionFlag.TraversableNorthWestBlocked) == 0)
                {
                    Check(_currentMapX - 1, _currentMapY, DirectionFlag.West, thisCost);
                }

                if (_currentMapY < GraphSize - 2 && _directions[_currentMapX, _currentMapY + 1] == 0 &&
                    (GetClippingFlag(absX, absY + 2, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                    (GetClippingFlag(absX + 1, absY + 2, z) & CollisionFlag.TraversableNorthWestBlocked) == 0)
                {
                    Check(_currentMapX, _currentMapY + 1, DirectionFlag.North, thisCost);
                }

                if (_currentMapX < GraphSize - 2 && _directions[_currentMapX + 1, _currentMapY] == 0 &&
                    (GetClippingFlag(absX + 2, absY, z) & CollisionFlag.TraversableSouthEastBlocked) == 0 &&
                    (GetClippingFlag(absX + 2, absY + 1, z) & CollisionFlag.TraversableNorthEastBlocked) == 0)
                {
                    Check(_currentMapX + 1, _currentMapY, DirectionFlag.East, thisCost);
                }

                if (_currentMapX > 0 && _currentMapY > 0 && _directions[_currentMapX - 1, _currentMapY - 1] == 0 &&
                    (GetClippingFlag(absX - 1, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX - 1, absY, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                    (GetClippingFlag(absX, absY - 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0)
                {
                    Check(_currentMapX - 1, _currentMapY - 1, DirectionFlag.SouthWest, thisCost);
                }

                if (_currentMapX > 0 && _currentMapY < GraphSize - 2 && _directions[_currentMapX - 1, _currentMapY + 1] == 0 &&
                    (GetClippingFlag(absX - 1, absY + 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX - 1, absY + 2, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                    (GetClippingFlag(absX, absY + 2, z) & CollisionFlag.TraversableNorthEastBlocked) == 0)
                {
                    Check(_currentMapX - 1, _currentMapY + 1, DirectionFlag.NorthWest, thisCost);
                }

                if (_currentMapX < GraphSize - 2 && _currentMapY > 0 && _directions[_currentMapX + 1, _currentMapY - 1] == 0 &&
                    (GetClippingFlag(absX + 1, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX + 2, absY, z) & CollisionFlag.TraversableNorthEastBlocked) == 0 &&
                    (GetClippingFlag(absX + 2, absY - 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0)
                {
                    Check(_currentMapX + 1, _currentMapY - 1, DirectionFlag.SouthEast, thisCost);
                }

                if (_currentMapX < GraphSize - 2 && _currentMapY < GraphSize - 2 && _directions[_currentMapX + 1, _currentMapY + 1] == 0 &&
                    (GetClippingFlag(absX + 1, absY + 2, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                    (GetClippingFlag(absX + 2, absY + 2, z) & CollisionFlag.TraversableNorthEastBlocked) == 0 &&
                    (GetClippingFlag(absX + 2, absY + 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0)
                {
                    Check(_currentMapX + 1, _currentMapY + 1, DirectionFlag.NorthEast, thisCost);
                }
            }
        }

        /// <summary>
        /// Finds the variable traversal.
        /// </summary>
        /// <param name="base">The base location.</param>
        /// <param name="to">To.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="targetSizeX">The target size x.</param>
        /// <param name="targetSizeY">The target size y.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="surroundings">The surroundings.</param>
        private void FindVariableTraversal(IVector3 @base, IVector3 to, int selfSize, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings)
        {
            var readPosition = 0;
            var z = @base.Z;
            while (_writePathPosition != readPosition)
            {
                _currentMapX = _queueX[readPosition];
                _currentMapY = _queueY[readPosition];
                readPosition = readPosition + 1 & 0xfff;

                var absX = @base.X + _currentMapX;
                var absY = @base.Y + _currentMapY;
                if (HasFoundPath(to, absX, absY, selfSize, targetSizeX, targetSizeY, shape, rotation, surroundings, z))
                {
                    FoundPath = true;
                    break;
                }

                // south
                var thisCost = _costs[_currentMapX, _currentMapY] + 1;
                if (_currentMapY > 0 && _directions[_currentMapX, _currentMapY - 1] == 0 &&
                    (GetClippingFlag(absX, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                    (GetClippingFlag(absX + (selfSize - 1), absY - 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0)
                {
                    for (var i = 1; i < selfSize - 1; i++)
                    {
                        if ((GetClippingFlag(absX + i, absY - 1, z) & CollisionFlag.TraversableVariableSouthBlocked) != 0)
                        {
                            goto west;
                        }
                    }

                    Check(_currentMapX, _currentMapY - 1, DirectionFlag.South, thisCost);
                }

                west:
                {
                    if (_currentMapX > 0 && _directions[_currentMapX - 1, _currentMapY] == 0 &&
                        (GetClippingFlag(absX - 1, absY, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                        (GetClippingFlag(absX - 1, absY + (selfSize - 1), z) & CollisionFlag.TraversableNorthWestBlocked) == 0)
                    {
                        for (var i = 1; i < selfSize - 1; i++)
                        {
                            if ((GetClippingFlag(absX - 1, absY + i, z) & CollisionFlag.TraversableVariableWestBlocked) != 0)
                            {
                                goto north;
                            }
                        }

                        Check(_currentMapX - 1, _currentMapY, DirectionFlag.West, thisCost);
                    }
                }
                north:
                {
                    if (_currentMapY < GraphSize - 2 && _directions[_currentMapX, _currentMapY + 1] == 0 &&
                        (GetClippingFlag(absX, absY + selfSize, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                        (GetClippingFlag(absX + (selfSize - 1), absY + selfSize, z) & CollisionFlag.TraversableNorthEastBlocked) == 0)
                    {
                        for (var i = 1; i < selfSize - 1; i++)
                        {
                            if ((GetClippingFlag(absX + i, absY + selfSize, z) & CollisionFlag.TraversableVariableNorthBlocked) != 0)
                            {
                                goto east;
                            }
                        }

                        Check(_currentMapX, _currentMapY + 1, DirectionFlag.North, thisCost);
                    }
                }
                east:
                {
                    if (_currentMapX < GraphSize - 2 && _directions[_currentMapX + 1, _currentMapY] == 0 &&
                        (GetClippingFlag(absX + selfSize, absY, z) & CollisionFlag.TraversableSouthEastBlocked) == 0 &&
                        (GetClippingFlag(absX + selfSize, absY + (selfSize - 1), z) & CollisionFlag.TraversableNorthEastBlocked) == 0)
                    {
                        for (var i = 1; i < selfSize - 1; i++)
                        {
                            if ((GetClippingFlag(absX + selfSize, absY + i, z) & CollisionFlag.TraversableVariableEastBlocked) != 0)
                            {
                                goto southWest;
                            }
                        }

                        Check(_currentMapX + 1, _currentMapY, DirectionFlag.East, thisCost);
                    }
                }
                southWest:
                {
                    if (_currentMapX > 0 && _currentMapY > 0 && _directions[_currentMapX - 1, _currentMapY - 1] == 0 &&
                        (GetClippingFlag(absX - 1, absY + (selfSize - 2), z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                        (GetClippingFlag(absX - 1, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                        (GetClippingFlag(absX + (selfSize - 2), absY - 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0)
                    {
                        for (var i = 1; i < selfSize - 1; i++)
                        {
                            if ((GetClippingFlag(absX - 1, absY + (i - 1), z) & CollisionFlag.TraversableWestBlocked) != 0 || (GetClippingFlag(absX + (i - 1), absY - 1, z) & CollisionFlag.TraversableVariableSouthBlocked) != 0)
                            {
                                goto northWest;
                            }
                        }

                        Check(_currentMapX - 1, _currentMapY + 1, DirectionFlag.SouthWest, thisCost);
                    }
                }
                northWest:
                {
                    if (_currentMapX > 0 && _currentMapY < GraphSize - 2 && _directions[_currentMapX - 1, _currentMapY + 1] == 0 &&
                        (GetClippingFlag(absX - 1, absY + 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                        (GetClippingFlag(absX - 1, absY + selfSize, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                        (GetClippingFlag(absX, absY + selfSize, z) & CollisionFlag.TraversableNorthEastBlocked) == 0)
                    {
                        for (var i = 1; i < selfSize - 1; i++)
                        {
                            if ((GetClippingFlag(absX - 1, absY + (i - 1), z) & CollisionFlag.TraversableWestBlocked) != 0 || (GetClippingFlag(absX + (i - 1), absY + selfSize, z) & CollisionFlag.TraversableVariableNorthBlocked) != 0)
                            {
                                goto southEast;
                            }
                        }

                        Check(_currentMapX - 1, _currentMapY + 1, DirectionFlag.NorthWest, thisCost);
                    }
                }
                southEast:
                {
                    if (_currentMapX < GraphSize - 2 && _currentMapY > 0 && _directions[_currentMapX + 1, _currentMapY - 1] == 0 &&
                        (GetClippingFlag(absX + 1, absY - 1, z) & CollisionFlag.TraversableSouthWestBlocked) == 0 &&
                        (GetClippingFlag(absX + selfSize, absY - 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0 &&
                        (GetClippingFlag(absX + selfSize, absY + (selfSize - 2), z) & CollisionFlag.TraversableNorthEastBlocked) == 0)
                    {
                        for (var i = 1; i < selfSize - 1; i++)
                        {
                            if ((GetClippingFlag(absX + selfSize, absY + (i - 1), z) & CollisionFlag.TraversableEastBlocked) != 0 || (GetClippingFlag(absX + i + 1, absY - 1, z) & CollisionFlag.TraversableVariableSouthBlocked) != 0)
                            {
                                goto northEast;
                            }
                        }

                        Check(_currentMapX - 1, _currentMapY + 1, DirectionFlag.SouthEast, thisCost);
                    }
                }
                northEast:
                {
                    if (_currentMapX < GraphSize - 2 && _currentMapY < GraphSize - 2 && _directions[_currentMapX + 1, _currentMapY + 1] == 0 &&
                        (GetClippingFlag(absX + 1, absY + selfSize, z) & CollisionFlag.TraversableNorthWestBlocked) == 0 &&
                        (GetClippingFlag(absX + selfSize, absY + selfSize, z) & CollisionFlag.TraversableNorthEastBlocked) == 0 &&
                        (GetClippingFlag(absX + selfSize, absY + 1, z) & CollisionFlag.TraversableSouthEastBlocked) == 0)
                    {
                        for (var i = 1; i < selfSize - 1; i++)
                        {
                            if ((GetClippingFlag(absX + i + 1, absY + selfSize, z) & CollisionFlag.TraversableNorthBlocked) != 0 || (GetClippingFlag(absX + selfSize, absY + i + 1, z) & CollisionFlag.TraversableVariableEastBlocked) != 0)
                            {
                                goto end;
                            }
                        }

                        Check(_currentMapX + 1, _currentMapY + 1, DirectionFlag.NorthEast, thisCost);
                    }
                }
                end:
                {
                    continue;
                }
            }
        }
    }
}