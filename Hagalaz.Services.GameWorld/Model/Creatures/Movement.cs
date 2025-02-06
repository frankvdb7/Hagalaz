using System;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Contains movement for creature.
    /// </summary>
    public class Movement : IMovement
    {
        /// <summary>
        /// Owner of this class.
        /// </summary>
        private readonly Creature _owner;

        /// <summary>
        /// Contains move queue.
        /// </summary>
        private readonly ILocation?[] _moveQueue = new ILocation[50];

        /// <summary>
        /// Contains move pointer in moveQueue.
        /// </summary>
        private byte _moveReadPointer = 0;

        /// <summary>
        /// Contains move write pointer in moveQueue.
        /// </summary>
        private byte _moveWritePointer = 0;

        /// <summary>
        /// Contains movement lock data.
        /// </summary>
        private int _movementLock = 0;

        /// <summary>
        /// The path finder
        /// </summary>
        private readonly IPathFinder _pathFinder;

        /// <summary>
        /// The movement type
        /// </summary>
        private MovementType _movementType;

        /// <summary>
        /// Contains creature movement type.
        /// </summary>
        /// <value>
        /// The type of the movement.
        /// </value>
        public MovementType MovementType
        {
            get => _movementType;
            set
            {
                if (_movementType == value)
                {
                    return;
                }

                _movementType = value;
                _owner.MovementTypeChanged(value);
            }
        }

        /// <summary>
        /// Contains temporary movement type.
        /// </summary>
        /// <value>
        /// The last type of the temporary movement.
        /// </value>
        public MovementType LastTemporaryMovementType { get; private set; }

        /// <summary>
        /// Contains boolean which tells if temporary movement type
        /// is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [temporary movement type enabled]; otherwise, <c>false</c>.
        /// </value>
        public bool TemporaryMovementTypeEnabled { get; private set; }

        /// <summary>
        /// Contains creature teleport data or null
        /// if it's not teleporting
        /// </summary>
        /// <value>
        /// The teleport data.
        /// </value>
        [MemberNotNullWhen(true, nameof(Teleporting))]
        private ITeleport? TeleportLocation { get; set; }

        /// <summary>
        /// Get's if creature is teleporting.
        /// </summary>
        /// <value>
        ///   <c>true</c> if teleporting; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.Exception">Can't set Teleporting to true!</exception>
        public bool Teleporting
        {
            get => TeleportLocation != null;
            private set
            {
                if (value) throw new Exception("Can't set Teleporting to true!");
                TeleportLocation = null;
            }
        }

        /// <summary>
        /// Get's if movement is locked.
        /// </summary>
        /// <value>
        ///   <c>true</c> if locked; otherwise, <c>false</c>.
        /// </value>
        public bool Locked => _movementLock > 0;

        /// <summary>
        /// Get's if character is moving.
        /// </summary>
        /// <value>
        ///   <c>true</c> if moving; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.NotSupportedException">Can't set Moving to true!</exception>
        public bool Moving
        {
            get => _moveReadPointer != _moveWritePointer;
            private set
            {
                if (value) throw new NotSupportedException("Can't set Moving to true!");
                _moveReadPointer = 0;
                _moveWritePointer = 0;
            }
        }

        /// <summary>
        /// Contains boolean if creature moved on this cycle.
        /// </summary>
        /// <value>
        ///   <c>true</c> if moved; otherwise, <c>false</c>.
        /// </value>
        public bool Moved { get; private set; }

        /// <summary>
        /// Contains boolean if creature teleported on this cycle.
        /// </summary>
        /// <value>
        ///   <c>true</c> if teleported; otherwise, <c>false</c>.
        /// </value>
        public bool Teleported { get; private set; }

        /// <summary>
        /// Creates new creature movement.
        /// </summary>
        /// <param name="owner"></param>
        public Movement(Creature owner)
        {
            _owner = owner;
            _movementType = MovementType.Walk;
            _pathFinder = owner.ServiceProvider.GetRequiredService<ISmartPathFinder>();
            owner.Mediator.ConnectHandler<ProfileValueChanged<bool>>(context =>
            {
                if (context.Message.Key == ProfileConstants.RunSettingsToggled)
                {
                    RunToggled(context.Message.NewValue);
                }
            });
        }

        private void RunToggled(bool toggle)
        {
            if (Locked)
            {
                return;
            }

            if (toggle)
            {
                MovementType = toggle ? MovementType.Run : MovementType.Walk;
            }
        }

        /// <summary>
        /// Enable's character temporary movement type.
        /// </summary>
        /// <param name="temporaryMovementType">Type of the temporary movement.</param>
        public void EnableTemporaryMovementType(MovementType temporaryMovementType)
        {
            LastTemporaryMovementType = temporaryMovementType;
            _owner.TemporaryMovementTypeEnabled(temporaryMovementType);
            TemporaryMovementTypeEnabled = true;
        }

        /// <summary>
        /// Peek's movement point.
        /// </summary>
        /// <returns></returns>
        private ILocation? PeekMovement() => _moveReadPointer == _moveWritePointer ? null : _moveQueue[_moveReadPointer];

        /// <summary>
        /// Write's movement location to queue.
        /// </summary>
        /// <param name="location"></param>
        private void WriteMovement(ILocation location)
        {
            _moveQueue[_moveWritePointer] = location;
            _moveWritePointer++;
        }

        /// <summary>
        /// Read's movement point.
        /// </summary>
        /// <returns></returns>
        private void NextMovement()
        {
            if (_moveReadPointer == _moveWritePointer)
            {
                return;
            }

            _moveQueue[_moveReadPointer] = null;
            _moveReadPointer++;
        }

        /// <summary>
        /// Teleports character to given location.
        /// </summary>
        /// <param name="teleport">Location where to teleport.</param>
        public void Teleport(ITeleport teleport)
        {
            Moving = false;
            TeleportLocation = teleport;
        }

        /// <summary>
        /// Clears movement queue.
        /// </summary>
        public void ClearQueue() => Moving = false;

        /// <summary>
        /// Adds to queue.
        /// </summary>
        /// <param name="path">The path.</param>
        public void AddToQueue(IPath path)
        {
            if (_movementLock > 0) return;
            ClearQueue();
            foreach (var point in path)
            {
                AddToQueue(point);
            }
        }

        /// <summary>
        /// Adds to queue.
        /// </summary>
        /// <param name="target">The location.</param>
        public void AddToQueue(IVector2 target)
        {
            if (_movementLock > 0) return;
            WriteMovement(Location.Create(target.X, target.Y, _owner.Location.Z, _owner.Location.Dimension));
        }

        /// <summary>
        /// Add's location to creatures movement queue.
        /// Does not add anything to the queue, if a movement lock has been set.
        /// </summary>
        /// <param name="target">Where to move.</param>
        public void AddToQueue(ILocation target)
        {
            if (_movementLock > 0) return;
            WriteMovement(target);
        }

        /// <summary>
        /// Processes character movement.
        /// </summary>
        public void Tick()
        {
            if (Moving)
            {
                if (_owner.HasState(StateType.Resting))
                {
                    _owner.RemoveState(StateType.Resting);
                    return;
                }

                var maxMove = MovementType switch
                {
                    MovementType.Run => 2,
                    MovementType.Warp => 0x3FFF / 2,
                    _ => 1
                };

                var finalLocation = _owner.Location.Clone();
                while (Moving && (maxMove > 0)) // move until we're out of available steps.
                {
                    var peek = PeekMovement();
                    if (peek == null)
                    {
                        continue;
                    }

                    var delta = Location.GetDelta(finalLocation, peek);
                    if (delta.Z != 0 || delta.Dimension != 0) break;
                    if (delta.X == 0 && delta.Y == 0)
                    {
                        NextMovement();
                        continue;
                    }

                    var max = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
                    var deltaX = delta.X;
                    var deltaY = delta.Y;

                    for (var i = 0; i < max; i++)
                    {
                        if ((deltaX != 0 || deltaY != 0) && maxMove-- <= 0) break;
                        if (deltaX < 0)
                            deltaX++;
                        else if (deltaX > 0) deltaX--;
                        if (deltaY < 0)
                            deltaY++;
                        else if (deltaY > 0) deltaY--;
                    }

                    var newLocation = Location.Create(peek.X - deltaX, peek.Y - deltaY, finalLocation.Z, finalLocation.Dimension);
                    // TODO - Correct most x, y coordinates for size >= 2
                    if (!_pathFinder.CheckStep(newLocation.X, newLocation.Y, newLocation.Z, delta.X, delta.Y, _owner.Size) || newLocation.Equals(finalLocation))
                        break; // we didn't move
                    finalLocation = newLocation;
                }

                if (!finalLocation.Equals(_owner.Location))
                {
                    Moved = true;
                    _owner.SetLocation(finalLocation);
                    if (MovementType == MovementType.Run && maxMove >= 1)
                    {
                        EnableTemporaryMovementType(MovementType.Walk);
                    }
                }
            }

            if (Teleporting)
            {
                // process teleport.
                Teleported = true;
                _owner.SetLocation(TeleportLocation!.Location);
                // Inform client that our teleport type is different then our walking type.
                if (TeleportLocation.Type != MovementType)
                {
                    EnableTemporaryMovementType(TeleportLocation.Type);
                }
            }
        }

        /// <summary>
        /// Resets movement data.
        /// </summary>
        public void Reset()
        {
            Teleporting = false;
            Moved = false;
            Teleported = false;
            TemporaryMovementTypeEnabled = false;
        }

        /// <summary>
        /// Lock's creature's movement.
        /// </summary>
        /// <param name="resetQueue">Wheter movement queue should be reset.</param>
        public void Lock(bool resetQueue)
        {
            _movementLock++;
            if (resetQueue) Moving = false;
        }

        /// <summary>
        /// Unlocks creature's movement.
        /// </summary>
        /// <param name="resetLock">if set to <c>true</c> [reset lock] completely.</param>
        public void Unlock(bool resetLock)
        {
            if (resetLock)
                _movementLock = 0;
            else
            {
                _movementLock--;
                if (_movementLock < 0) _movementLock = 0;
            }
        }
    }
}