using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.GameObjects
{
    /// <summary>
    /// </summary>
    public class Lodestone : GameObjectScript
    {
        private readonly ILodestoneService _lodestoneService;
        private readonly IRegionUpdateBuilder _regionUpdateBuilder;
        private readonly IMapRegionService _mapRegionService;

        public Lodestone(ILodestoneService lodestoneService, IRegionUpdateBuilder regionUpdateBuilder, IMapRegionService mapRegionService)
        {
            _lodestoneService = lodestoneService;
            _regionUpdateBuilder = regionUpdateBuilder;
            _mapRegionService = mapRegionService;
        }
        
        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.QueueTask(cancellationToken => BeginTeleport(clicker, cancellationToken));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        private async Task BeginTeleport(ICharacter character, System.Threading.CancellationToken cancellationToken)
        {
            var gameObjectId = Owner.Id;
            var location = Owner.Location;
            var lodeStone = await _lodestoneService.FindByGameObjectId(gameObjectId);
            cancellationToken.ThrowIfCancellationRequested();
            if (lodeStone == null)
            {
                return;
            }
            character.AddState(new TeleportingState { TicksLeft = 1 });
            var update = _regionUpdateBuilder.Create().WithLocation(location).WithGraphic(Graphic.Create(3019)).Build();
            _mapRegionService.QueueUpdate(update);
            // TODO - Show cutscene
        }
    }
}
