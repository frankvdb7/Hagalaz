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

        public Lodestone(ILodestoneService lodestoneService, IRegionUpdateBuilder regionUpdateBuilder)
        {
            _lodestoneService = lodestoneService;
            _regionUpdateBuilder = regionUpdateBuilder;
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
                clicker.QueueTask(() => BeginTeleport(clicker));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        private async Task BeginTeleport(ICharacter character)
        {
            // StateType state = (StateType)((int)(this.owner.Id - 69828 + 117));
            var lodeStone = await _lodestoneService.FindByGameObjectId(Owner.Id);
            if (lodeStone == null)
            {
                return;
            }
            character.AddState(new TeleportingState { TicksLeft = int.MaxValue });
            var update = _regionUpdateBuilder.Create().WithLocation(Owner.Location).WithGraphic(Graphic.Create(3019)).Build();
            Owner.Region.QueueUpdate(update);
            // TODO - Show cutscene
        }
    }
}