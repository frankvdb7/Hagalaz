using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class HintIconBuilder : IHintIconBuilder, IHintIconBuild, IHintIconOptional, IHintIconEntityOptional, IHintIconLocationOptional, IHintIconType
    {
        private int? _creatureIndex;
        private bool _isCharacter;
        private ILocation? _location;

        private int _modelId = -1;
        private int _viewDistance = -1;
        private int _height = 100;
        private int _flashSpeed = 2500;
        private HintIconDirection _direction = HintIconDirection.Center;
        private int _arrowId;


        public IHintIconType Create() => new HintIconBuilder();

        public IHintIcon Build()
        {
            if (_creatureIndex == null)
            {
                return new LocationHintIcon
                {
                    ModelId = _modelId,
                    Target = _location!,
                    ViewDistance = _viewDistance,
                    Height = _height,
                    ArrowId = _arrowId,
                    Direction = _direction
                };
            }

            if (_location == null)
            {
                return new CreatureHintIcon
                {
                    ModelId = _modelId,
                    IsCharacter = _isCharacter,
                    TargetIndex = _creatureIndex.Value!,
                    ArrowId = _arrowId,
                    FlashSpeed = _flashSpeed
                };
            }

            return null!;
        }

        public IHintIconOptional WithModelId(int modelId)
        {
            _modelId = modelId;
            return this;
        }

        public IHintIconOptional WithArrowId(int arrowId)
        {
            _arrowId = arrowId;
            return this;
        }

        public IHintIconEntityOptional WithFlashSpeed(int flashSpeed)
        {
            _flashSpeed = flashSpeed;
            return this;
        }

        public IHintIconLocationOptional WithHeight(int height)
        {
            _height = height;
            return this;
        }

        public IHintIconLocationOptional WithViewDistance(int viewDistance)
        {
            _viewDistance = viewDistance;
            return this;
        }

        public IHintIconLocationOptional WithDirection(HintIconDirection direction)
        {
            _direction = direction;
            return this;
        }

        public IHintIconLocationOptional AtLocation(ILocation location)
        {
            _location = location;
            return this;
        }

        public IHintIconEntityOptional AtEntity(IEntity entity)
        {
            if (entity is ICreature creature)
            {
                _creatureIndex = creature.Index;
                _isCharacter = creature is ICharacter;
            }
            else _location = entity.Location;
            return this;
        }
    }
}