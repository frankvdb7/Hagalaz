using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly EquipmentStore _equipment;

        public EquipmentService(EquipmentStore equipment)
        {
            _equipment = equipment;
        }

        public IEquipmentDefinition FindEquipmentDefinitionById(int itemId) => _equipment.GetOrAdd(itemId);
    }
}