using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class MovementBuilder : IMovementBuilder, IMovementBuild, IMovementOptional, IMovementLocationEnd, IMovementLocationStart
    {
        private ILocation? _start;
        private ILocation? _end;
        private int _startSpeed;
        private int _endSpeed;
        private FaceDirection? _faceDirection;
        private DirectionFlag? _direction;

        public IMovementLocationStart Create() => new MovementBuilder();

        public IForceMovement Build()
        {
            var faceDirection = _faceDirection ?? _direction?.ToFaceDirection() ?? FaceDirection.None;
            return new ForceMovement
            {
                StartLocation = _start!,
                EndLocation = _end!,
                StartSpeed = _startSpeed,
                EndSpeed = _endSpeed,
                FaceDirection = faceDirection,
            };
        }

        public IMovementOptional WithFaceDirection(FaceDirection direction)
        {
            _faceDirection = direction;
            return this;
        }

        public IMovementOptional WithDirection(DirectionFlag direction)
        {
            _direction = direction;
            return this;
        }

        public IMovementOptional WithStartSpeed(int speed)
        {
            _startSpeed = speed;
            return this;
        }

        public IMovementOptional WithEndSpeed(int speed)
        {
            _endSpeed = speed;
            return this;
        }

        public IMovementOptional WithEnd(ILocation location)
        {
            _end = location;
            return this;
        }

        public IMovementLocationEnd WithStart(ILocation location)
        {
            _start = location;
            return this;
        }
    }
}